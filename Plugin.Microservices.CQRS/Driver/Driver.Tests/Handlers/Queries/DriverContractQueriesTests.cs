using Driver.Core.CQRS.Queries;
using Driver.Core.Enums;
using Driver.Handlers.CQRS.Queries;
using NetTopologySuite.Geometries;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

/// <summary>
/// The two service-to-service queries Ride sends. Unlike GetDriverStatus they carry no caller
/// identity and are not ownership-gated — they exist precisely so a cross-service call does not
/// have to borrow an end user's identity to read the narrow field it needs.
/// </summary>
public class DriverContractQueriesTests
{
  [Fact]
  public async Task GetDriverAvailability_ReportsAnAvailableDriver()
  {
    var (context, _) = TestBase.CreateTestServices();
    var id = Guid.NewGuid();
    context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id, Status = DriverStatus.Available });
    await context.SaveChangesAsync();

    var result = await new GetDriverAvailabilityHandler(context).Handle(new GetDriverAvailability { DriverId = id }, CancellationToken.None);

    Assert.True(result.Exists);
    Assert.True(result.IsAvailable);
  }

  [Fact]
  public async Task GetDriverAvailability_ReportsADriverAlreadyOnARide()
  {
    var (context, _) = TestBase.CreateTestServices();
    var id = Guid.NewGuid();
    context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id, Status = DriverStatus.OnRide });
    await context.SaveChangesAsync();

    var result = await new GetDriverAvailabilityHandler(context).Handle(new GetDriverAvailability { DriverId = id }, CancellationToken.None);

    Assert.True(result.Exists);
    Assert.False(result.IsAvailable);
  }

  [Fact]
  public async Task GetDriverAvailability_ReportsAbsence_RatherThanThrowing()
  {
    // An unknown DriverId is a bad client request for Ride to turn into a 404, not a fault here.
    var (context, _) = TestBase.CreateTestServices();

    var result = await new GetDriverAvailabilityHandler(context).Handle(new GetDriverAvailability { DriverId = Guid.NewGuid() }, CancellationToken.None);

    Assert.False(result.Exists);
    Assert.False(result.IsAvailable);
  }

  [Fact]
  public async Task GetDriverLocation_ReturnsLastKnownLocation()
  {
    var (context, _) = TestBase.CreateTestServices();
    var id = Guid.NewGuid();
    var location = new Point(9.19, 45.46) { SRID = 4326 };
    context.Drivers.Add(new Driver.Handlers.Models.Driver { Id = id, LastLocation = location });
    await context.SaveChangesAsync();

    var result = await new GetDriverLocationHandler(context).Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    Assert.Equal(location, result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ReturnsNullLocation_WhenDriverIsUnknown()
  {
    var (context, _) = TestBase.CreateTestServices();

    var result = await new GetDriverLocationHandler(context).Handle(new GetDriverLocation { DriverId = Guid.NewGuid() }, CancellationToken.None);

    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }
}
