using System.Security.Claims;
using Driver.Api.Controllers;
using Driver.Core.Dtos;
using Driver.Core.Ports.Primary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Controllers;

/// <summary>
/// The kilometre-to-metre conversion is the one piece of the matching path that lives in the
/// primary HTTP adapter, so no use-case test can cover it: DistanceThresholdInMeters feeds a
/// geography Distance() comparison, which SQL Server evaluates in metres.
/// </summary>
public class DriversControllerTests
{
  private readonly ISearchBestDriverUseCase _searchBestDriver = Substitute.For<ISearchBestDriverUseCase>();

  private DriversController NewController() => new(
    Substitute.For<IAddDriverUseCase>(),
    Substitute.For<IUpdateAvailabilityUseCase>(),
    Substitute.For<IUpdateLocationUseCase>(),
    Substitute.For<IGetDriverStatusUseCase>(),
    _searchBestDriver)
  {
    ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext
      {
        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"))
      }
    }
  };

  [Theory]
  [InlineData(1, 1000)]
  [InlineData(5, 5000)]
  [InlineData(25, 25000)]
  public async Task SearchBestDriver_ConvertsKilometresToMetres(int requestedKm, int expectedMetres)
  {
    // Arrange
    var controller = NewController();
    _searchBestDriver.Handle(Arg.Any<SearchBestDriver>(), Arg.Any<CancellationToken>())
      .Returns(new List<SearchBestDriverResponse>());

    // Act
    await controller.SearchBestDriver(new SearchBestDriverRequest
    {
      Location = new Point(0, 0),
      DistanceThresholdInKm = requestedKm
    }, CancellationToken.None);

    // Assert
    await _searchBestDriver.Received(1).Handle(
      Arg.Is<SearchBestDriver>(q => q.DistanceThresholdInMeters == expectedMetres),
      Arg.Any<CancellationToken>());
  }
}
