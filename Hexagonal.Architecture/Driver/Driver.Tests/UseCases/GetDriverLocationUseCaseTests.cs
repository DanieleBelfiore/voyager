using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using NetTopologySuite.Geometries;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.UseCases;

public class GetDriverLocationUseCaseTests
{
  private readonly IDriverRepository _repository;
  private readonly GetDriverLocationUseCase _useCase;

  public GetDriverLocationUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _useCase = new GetDriverLocationUseCase(_repository);
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
    var result = await _useCase.Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(location, result.LastLocation);
  }

  [Fact]
  public async Task GetDriverLocation_ShouldReturnNullLocation_WhenDriverIsUnknown()
  {
    // Arrange — an unknown DriverId is the caller's bad request to handle, not a fault here,
    // so this mirrors GetDriverAvailabilityUseCase and reports absence instead of throwing.
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((DriverEntity)null);

    // Act
    var result = await _useCase.Handle(new GetDriverLocation { DriverId = Guid.NewGuid() }, CancellationToken.None);

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
    var result = await _useCase.Handle(new GetDriverLocation { DriverId = id }, CancellationToken.None);

    // Assert
    Assert.NotNull(result);
    Assert.Null(result.LastLocation);
  }
}
