using System.Net.Http.Headers;
using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ride.Handlers.Models;
using Xunit;

namespace Ride.IntegrationTests
{
  public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
  {
    private readonly IntegrationTestWebAppFactory _factory;
    private IServiceScope _scope;
    protected RideContext Context;
    private IUserManager UserManager;
    protected HttpClient Client;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
      _factory = factory;

      CreateNewScope();
    }

    private void CreateNewScope()
    {
      _scope.Dispose();
      _scope = _factory.Services.CreateScope();

      InitializeServices(_scope.ServiceProvider);
    }

    private void InitializeServices(IServiceProvider services)
    {
      services.GetRequiredService<IMediator>();
      Context = services.GetRequiredService<RideContext>();
      UserManager = services.GetRequiredService<IUserManager>();
      Client = _factory.CreateClient();
    }

    protected void RefreshContext()
    {
      CreateNewScope();
    }

    protected async Task ResetDatabaseAsync()
    {
      using var scope = _factory.Services.CreateScope();
      var scopedServices = scope.ServiceProvider;
      var db = scopedServices.GetRequiredService<RideContext>();
      await db.Database.EnsureDeletedAsync();
      await db.Database.EnsureCreatedAsync();
    }

    private async Task<string> GetTokenAsync(string userName, string password)
    {
      var tokenRequest = new Dictionary<string, string>
      {
        ["grant_type"] = "password",
        ["client_id"] = "voyager_app",
        ["username"] = userName,
        ["password"] = password
      };

      var response = await Client.PostAsync("/connect/token", new FormUrlEncodedContent(tokenRequest));

      if (!response.IsSuccessStatusCode)
        throw new Exception($"failed_to_get_token: {await response.Content.ReadAsStringAsync()}");

      var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync());

      return tokenResponse == null ? throw new Exception("failed_to_get_token") : tokenResponse.AccessToken;
    }

    protected async Task InitializeAuthenticatedClient()
    {
      var user = new VoyagerUser
      {
        UserName = "user@test.it",
        Email = "user@test.it",
        FirstName = "User",
        LastName = "Test",
        IsDriver = false
      };

      const string password = "Aa123456!";

      await UserManager.CreateAsync(user, password);

      await UserManager.FindByIdAsync(user.Id.ToString());

      var token = await GetTokenAsync(user.UserName, password);

      Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public class TokenResponse
    {
      [JsonProperty("access_token")]
      public string AccessToken { get; set; }

      [JsonProperty("token_type")]
      public string TokenType { get; set; }

      [JsonProperty("expires_in")]
      public int ExpiresIn { get; set; }
    }
  }
}
