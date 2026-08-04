using Voyager.TestInfra;
using Xunit;

namespace Voyager.Host.IntegrationTests;

/// <summary>
/// One container set and one host for the whole assembly. The monolith composes all four modules
/// into a single process, so a single <see cref="VoyagerHost{TProgram}"/> covers every concern
/// these tests reach for — including the mediator hops that the service-per-process variants have
/// to route over RabbitMQ, and the SignalR hub.
/// </summary>
public class VoyagerAppFixture : IAsyncLifetime
{
  public VoyagerContainers Containers { get; } = new();
  public DatabaseResetter Databases { get; } = new();
  public VoyagerHost<Program> App { get; private set; }

  public async Task InitializeAsync()
  {
    await Containers.InitializeAsync();

    // Before the host is created: some registrations bind configuration eagerly and never see a
    // source added later. See VoyagerContainers.ExportToEnvironment.
    Containers.ExportToEnvironment();

    App = new VoyagerHost<Program>(Containers.Configuration()).Start();
  }

  public async Task DisposeAsync()
  {
    if (App != null)
      await App.DisposeAsync();

    await Containers.DisposeAsync();
  }

  /// <summary>
  /// Clears the business catalogues between tests. The identity catalogue is left alone on
  /// purpose — see <see cref="DatabaseResetter"/> — and tests get their isolation from the unique
  /// user <see cref="VoyagerAuth.RegisterAsync"/> creates per call.
  /// </summary>
  public Task ResetAsync()
  {
    return Task.WhenAll(
      Databases.ResetAsync(Containers.SqlConnectionString("driver")),
      Databases.ResetAsync(Containers.SqlConnectionString("ride")));
  }

  public HttpClient NewClient()
  {
    return App.CreateClient();
  }

  public async Task<HttpClient> NewAuthenticatedClientAsync(bool isDriver)
  {
    var client = NewClient();
    var user = await VoyagerAuth.RegisterAsync(client, isDriver);
    client.Authenticate(user);

    return client;
  }
}

[CollectionDefinition(Name)]
public class VoyagerCollection : ICollectionFixture<VoyagerAppFixture>
{
  public const string Name = "voyager-monolith";
}

[Collection(VoyagerCollection.Name)]
public abstract class VoyagerIntegrationTest(VoyagerAppFixture fixture)
{
  protected VoyagerAppFixture Fixture { get; } = fixture;
}
