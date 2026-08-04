using System.Net.Http.Headers;
using Driver.Handlers.Models;
using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using Hikyaku;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace Driver.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public abstract class BaseIntegrationTest
{
  private readonly IntegrationTestWebAppFactory _factory;
  private IServiceScope? _scope;
  protected IntegrationTestWebAppFactory Factory => _factory;
  protected DriverContext Context;
  private IUserManager UserManager;
  protected HttpClient Client;

  protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
  {
    _factory = factory;

    CreateNewScope();
  }

  private void CreateNewScope()
  {
    _scope?.Dispose();
    _scope = _factory.Services.CreateScope();

    InitializeServices(_scope.ServiceProvider);
  }

  private void InitializeServices(IServiceProvider services)
  {
    services.GetRequiredService<IHikyaku>();
    Context = services.GetRequiredService<DriverContext>();
    UserManager = services.GetRequiredService<IUserManager>();
    Client = _factory.CreateClient();
  }

  protected void RefreshContext()
  {
    CreateNewScope();
  }

  protected Task ResetDatabaseAsync()
  {
    return _factory.ResetDatabaseAsync();
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

  protected Task<Guid> InitializeAuthenticatedClient()
  {
    return InitializeAuthenticatedClient(isDriver: true);
  }

  /// <summary>
  /// Registers a fresh user and puts their bearer token on <see cref="Client"/>, returning the id
  /// so callers can seed rows against it. The name is unique per call: the identity catalogue is
  /// never truncated between tests — it carries the OpenIddict client registration the token
  /// endpoint needs — so a fixed name would collide the moment a second test ran.
  /// </summary>
  protected async Task<Guid> InitializeAuthenticatedClient(bool isDriver)
  {
    var name = $"{(isDriver ? "driver" : "rider")}-{Guid.NewGuid():N}@voyager.test";

    var user = new VoyagerUser
    {
      UserName = name,
      Email = name,
      FirstName = "Test",
      LastName = isDriver ? "Driver" : "Rider",
      IsDriver = isDriver
    };

    const string password = "Aa123456!";

    await UserManager.CreateAsync(user, password);

    await UserManager.FindByIdAsync(user.Id.ToString());

    var token = await GetTokenAsync(user.UserName, password);

    Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    return user.Id;
  }

  /// <summary>
  /// A second client on the same host with no credentials, for asserting what an anonymous caller
  /// gets. <see cref="Client"/> keeps whatever token the test put on it.
  /// </summary>
  protected HttpClient NewAnonymousClient()
  {
    return _factory.CreateClient();
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
