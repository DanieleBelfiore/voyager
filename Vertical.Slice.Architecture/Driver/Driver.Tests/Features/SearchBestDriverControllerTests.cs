using System.Security.Claims;
using Driver.Api.Features.SearchBestDriver;
using Hikyaku;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Features;

/// <summary>
/// The kilometre-to-metre conversion is the one piece of the matching path that lives in the
/// controller half of the slice, so no handler test can cover it: DistanceThresholdInMeters feeds
/// a geography Distance() comparison, which SQL Server evaluates in metres.
/// </summary>
public class SearchBestDriverControllerTests
{
  private readonly IHikyaku _mediator = Substitute.For<IHikyaku>();

  private SearchBestDriverController NewController() => new(_mediator)
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
  public async Task Search_ConvertsKilometresToMetres(int requestedKm, int expectedMetres)
  {
    // Arrange
    var controller = NewController();
    _mediator.Send(Arg.Any<SearchBestDriver>(), Arg.Any<CancellationToken>())
      .Returns(new List<SearchBestDriverResponse>());

    // Act
    await controller.Search(new SearchBestDriverRequest
    {
      Location = new Point(0, 0),
      DistanceThresholdInKm = requestedKm
    }, CancellationToken.None);

    // Assert
    await _mediator.Received(1).Send(
      Arg.Is<SearchBestDriver>(q => q.DistanceThresholdInMeters == expectedMetres),
      Arg.Any<CancellationToken>());
  }
}
