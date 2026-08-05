using Hikyaku;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Driver.Module.Entities;
using Driver.Module.Features.SearchBestDriver;
using Ride.Module.Features.GetRideETA;
using Ride.Module.Shared;
using Voyager.Contracts.Identity;
using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// A module answering another module's query. The request type lives in
/// <c>Commons/Voyager.Contracts</c> and is dispatched with the same <c>IHikyaku.Send</c> call the
/// service-per-process variants route over RabbitMQ — this variant just resolves it in-process.
/// The point of testing it here is that nothing in the calling module references the handler:
/// dispatch works only if every module assembly really was scanned at composition, which is a
/// property of the host and invisible to a unit test that injects a mocked mediator.
/// </summary>
public class CrossModuleDispatchTests(VoyagerAppFixture fixture) : VoyagerIntegrationTest(fixture)
{
  private static readonly Point Rome = new(12.4964, 41.9028) { SRID = 4326 };
  private static readonly Point AcrossTown = new(12.5451, 41.8992) { SRID = 4326 };

  /// <summary>
  /// Routing on its own: an unregistered request type throws rather than returning empty, so a
  /// dictionary coming back at all is the assertion. It is empty here on purpose — Identity
  /// deliberately omits users nobody has rated yet, so that <c>SearchBestDriver</c>'s
  /// "unrated defaults to the midpoint" fallback fires instead of ranking every new driver last.
  /// </summary>
  [Fact]
  public async Task AContractRequest_IsRoutedToTheModuleThatOwnsIt()
  {
    var client = Fixture.NewClient();
    var user = await VoyagerAuth.RegisterAsync(client, isDriver: true);

    using var scope = Fixture.App.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IHikyaku>();

    var ratings = await mediator.Send(new GetUsersRatings { UserIds = [user.Id] });

    Assert.NotNull(ratings);
  }

  /// <summary>
  /// The same hop through the caller that actually depends on it: driver matching scores
  /// candidates on rating, which only Identity holds.
  /// </summary>
  [Fact]
  public async Task DriverMatching_ScoresCandidates_UsingRatingsFromTheIdentityModule()
  {
    await Fixture.ResetAsync();

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Rome })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    var rider = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var response = await rider.PostAsync("api/v1/drivers/search",
      VoyagerJson.Content(new SearchBestDriverRequest { Location = Rome, DistanceThresholdInKm = 5 })).ShouldSucceed();

    var match = Assert.Single(await response.ReadAsync<List<SearchBestDriverResponse>>());

    Assert.Equal(driverUser.Id, match.DriverId);
    // Distance is ~0 for a driver sitting on the search point, so the score is carried entirely by
    // the rating term — a zero here means the ratings lookup came back empty.
    Assert.True(match.Score > 0, $"Candidate scored {match.Score}; the rating term contributed nothing.");
  }

  /// <summary>
  /// The Ride module holds no driver location, so an ETA can only come back if its
  /// GetDriverLocation request was routed to a handler in the Driver module. Unit tests inject a
  /// mocked mediator, which is why a contract nobody handles stayed invisible until here.
  /// </summary>
  [Fact]
  public async Task ARidesETA_IsAnsweredByTheDriverModule()
  {
    await Fixture.ResetAsync();

    var driver = Fixture.NewClient();
    var driverUser = await VoyagerAuth.RegisterAsync(driver, isDriver: true);
    driver.Authenticate(driverUser);

    await driver.PostAsync("api/v1/drivers", VoyagerJson.Content(new { })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/location", VoyagerJson.Content(new { Location = Rome })).ShouldSucceed();
    await driver.PutAsync("api/v1/drivers/availability", VoyagerJson.Content(new { Status = DriverStatus.Available })).ShouldSucceed();

    var rider = Fixture.NewClient();
    var riderUser = await VoyagerAuth.RegisterAsync(rider, isDriver: false);
    rider.Authenticate(riderUser);

    var ride = await (await rider.PostAsync("api/v1/rides", VoyagerJson.Content(new
    {
      DriverId = driverUser.Id,
      PickupLocation = AcrossTown,
      DropoffLocation = Rome
    })).ShouldSucceed()).ReadAsync<RideDetailsResponse>();

    var eta = await (await rider.GetAsync($"api/v1/rides/{ride.Id}/eta").ShouldSucceed()).ReadAsync<ETAResponse>();

    Assert.NotNull(eta.EstimatedArrivalMinutes);
    // An unrouted query degrades to a null pair, and a driver sitting on the pickup would measure
    // zero either way — this driver is a town away, so a positive distance is the actual proof.
    Assert.True(eta.DistanceKm > 0, $"ETA came back with distance {eta.DistanceKm}; no driver location crossed the module boundary.");
  }
}
