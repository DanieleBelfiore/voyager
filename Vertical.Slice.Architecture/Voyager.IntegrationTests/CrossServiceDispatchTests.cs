using Hikyaku;
using Microsoft.Extensions.DependencyInjection;
using Voyager.Contracts.Identity;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.IntegrationTests;

/// <summary>
/// One service answering another service's query over RabbitMQ.
/// </summary>
/// <remarks>
/// This is the mechanism the whole variant rests on and the one no unit test can approach.
/// <c>IHikyaku.Send</c> resolves locally when a handler is registered and otherwise hands the
/// request to Kaido, which routes it by the request type's full name. Two consequences only show
/// up with both services running: the type has to be the very same type on both ends of the wire —
/// structurally identical is not enough, which is why these contracts live in
/// <c>Commons/Voyager.Contracts</c> — and the responding service has to have a consumer bound to
/// that routing key at the time the request goes out.
/// </remarks>
public class CrossServiceDispatchTests(VoyagerStackFixture fixture) : VoyagerIntegrationTest(fixture)
{
  /// <summary>
  /// Sent from inside the Driver host, which registers no handler for this contract. A reply can
  /// therefore only have come from Identity, across the broker.
  /// </summary>
  [Fact]
  public async Task AContractRequest_IsAnsweredByTheServiceThatOwnsIt_AcrossTheBroker()
  {
    var user = await Fixture.RegisterAsync(isDriver: true);

    using var scope = Fixture.Driver.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IHikyaku>();

    var ratings = await mediator.Send(new GetUsersRatings { UserIds = [user.Id] });

    // Empty is the correct answer for a user nobody has rated — Identity omits them so that
    // matching falls back to the midpoint instead of ranking every new driver last. The
    // assertion is that a reply came back at all: an unrouted request throws or times out.
    Assert.NotNull(ratings);
  }

  /// <summary>
  /// The same hop through the caller that depends on it. Driver matching scores candidates on
  /// rating, and only Identity holds ratings.
  /// </summary>
  [Fact]
  public async Task DriverMatching_ScoresCandidates_UsingRatingsFetchedFromIdentity()
  {
    await Fixture.ResetAsync();

    var driver = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var rider = Fixture.ClientFor(VoyagerService.Driver, await Fixture.RegisterAsync(isDriver: false));

    var response = await rider.PostAsync("api/v1/drivers/search", VoyagerJson.Content(
      new SearchBestDriverPayload { Location = DriverWorkflow.Rome, DistanceThresholdInKm = 5 })).ShouldSucceed();

    var match = Assert.Single(await response.ReadAsync<List<SearchBestDriverResult>>());

    Assert.Equal(driver.Id, match.DriverId);
    // Distance is ~0 for a driver sitting on the search point, so the score is carried entirely
    // by the rating term — zero here means the ratings request came back with nothing usable.
    Assert.True(match.Score > 0, $"Candidate scored {match.Score}; the rating term contributed nothing.");
  }

  /// <summary>
  /// Ride owns no driver location, so an ETA can only come back if its GetDriverLocation request
  /// reached a handler in the Driver service. Every unit test substitutes that port, which is why
  /// a contract with no handler on the other end stayed invisible until here.
  /// </summary>
  [Fact]
  public async Task ARidesETA_IsAnsweredByTheDriverService_AcrossTheBroker()
  {
    await Fixture.ResetAsync();

    var driverUser = await Fixture.RegisterAvailableDriverAsync(DriverWorkflow.Rome);
    var rider = Fixture.ClientFor(VoyagerService.Ride, await Fixture.RegisterAsync(isDriver: false));

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new RequestRidePayload
    {
      DriverId = driverUser.Id,
      PickupLocation = DriverWorkflow.AcrossTown,
      DropoffLocation = DriverWorkflow.Rome
    })).ShouldSucceed()).ReadAsync<RideDetailsResult>();

    var eta = await (await rider.GetAsync($"api/v1/rides/{ride.Id}/eta").ShouldSucceed()).ReadAsync<ETAResult>();

    Assert.NotNull(eta.EstimatedArrivalMinutes);
    // An unanswered query degrades to a null pair, and a driver sitting on the pickup would score
    // zero distance either way — this driver is a town away, so a positive distance is the proof.
    Assert.True(eta.DistanceKm > 0, $"ETA came back with distance {eta.DistanceKm}; no driver location crossed the broker.");
  }
}
