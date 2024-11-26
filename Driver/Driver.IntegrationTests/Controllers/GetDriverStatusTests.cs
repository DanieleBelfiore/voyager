using System.Net;
using FluentAssertions;
using Xunit;

namespace Driver.IntegrationTests.Controllers
{
  public class GetDriverStatusTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
  {
    private async Task InitializeTestAsync(Guid id)
    {
      await ResetDatabaseAsync();
      await InitializeAuthenticatedClient();
      await InitializeTestSeedDataAsync(id);
    }

    private async Task InitializeTestSeedDataAsync(Guid id)
    {
      Context.Drivers.Add(new Handlers.Models.Driver { Id = id });

      await Context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetDriverStatus_ShouldReturnCorrectResponseCode_WhenRequestIsValid()
    {
      var id = Guid.NewGuid();

      // Arrange
      await InitializeTestAsync(id);

      // Act
      var response = await Client.GetAsync($"api/v1/drivers/{id}");

      // Assert
      response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
  }
}
