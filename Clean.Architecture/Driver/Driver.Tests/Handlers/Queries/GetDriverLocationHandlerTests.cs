using Driver.Application.CQRS.Queries;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using NetTopologySuite.Geometries;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.Handlers.Queries;

public class GetDriverLocationHandlerTests
{
  private readonly IDriverRepository _repository;
  private readonly GetDriverLocationHandler _handler;

  public GetDriverLocationHandlerTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _handler = new GetDriverLocationHandler(_repository);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnLastKnownLocation()
  {
    // Arrange
    var id = Guid.NewGuid();
    var driver = new DriverEntity(id);
    var location = new Point(9.19, 45.46) { SRID = 4326 };
    driver.UpdateLocation(location);
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(driver);

    // Act
    var result = await _handler.Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(location, result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnNullLocation_WhenDriverIsUnknown()
  {
    // Arrange — an unknown DriverId is the caller's bad request to handle, not a fault here,
    // so this mirrors GetDriverAvailabilityHandler and reports absence instead of throwing.
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity)null);

    // Act
    var result = await _handler.Handle(new GetDriverLocation { DriverId = Guid.NewGuid() }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnNullLocation_WhenDriverNeverReportedOne()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    // Act
    var result = await _handler.Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }
}
