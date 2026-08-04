using Xunit;

namespace Ride.IntegrationTests;

/// <summary>
/// One set of containers for the whole assembly. With <c>IClassFixture</c> every test class got
/// its own <see cref="IntegrationTestWebAppFactory"/>, and therefore its own SQL Server and
/// RabbitMQ — two container starts per class, which does not scale past a couple of tests.
/// The collection also serialises the classes, so the shared database reset cannot race.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory>
{
  public const string Name = "ride-integration";
}
