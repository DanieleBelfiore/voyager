using System.Net;
using FluentAssertions;
using Xunit;

namespace Ride.IntegrationTests.Controllers
{
  public class GetRideDetailsTests : BaseIntegrationTest
  {
    public GetRideDetailsTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    private async Task InitializeTestAsync(Guid id)
    {
      await ResetDatabaseAsync();
      await InitializeAuthenticatedClient();
      await InitializeTestSeedDataAsync(id);
    }

    private async Task InitializeTestSeedDataAsync(Guid id)
    {
      Context.Rides.Add(new Handlers.Models.Ride { Id = id });

      await Context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetRideDetails_ShouldReturnCorrectResponseCode_WhenRequestIsValid()
    {
      var id = Guid.NewGuid();

      // Arrange
      await InitializeTestAsync(id);

      // Act
      var response = await Client.GetAsync($"api/v1/rides/{id}");

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
  }
}
