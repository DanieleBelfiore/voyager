using System.Net;
using Xunit;

namespace Ride.IntegrationTests.Controllers;

public class GetRideDetailsTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
  private async Task InitializeTestAsync(Guid id)
  {
    await ResetDatabaseAsync();
    var userId = await InitializeAuthenticatedClient();
    await InitializeTestSeedDataAsync(id, userId);
  }

  private async Task InitializeTestSeedDataAsync(Guid id, Guid userId)
  {
    Context.Rides.Add(new Handlers.Models.Ride { Id = id, UserId = userId });

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
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }
}
