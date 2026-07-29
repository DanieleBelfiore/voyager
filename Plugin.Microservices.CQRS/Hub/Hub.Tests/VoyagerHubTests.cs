using System.Security.Claims;
using Driver.Core.CQRS.Commands;
using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Queries;
using Ride.Core.Dtos;
using Xunit;

namespace Hub.Tests;

public class VoyagerHubTests
{
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly IGroupManager _groups = Substitute.For<IGroupManager>();
  private readonly IHubCallerClients<IVoyagerShareClient> _clients = Substitute.For<IHubCallerClients<IVoyagerShareClient>>();
  private readonly IVoyagerShareClient _groupClient = Substitute.For<IVoyagerShareClient>();
  private readonly HubCallerContext _context = Substitute.For<HubCallerContext>();
  private readonly VoyagerHub _hub;

  public VoyagerHubTests()
  {
    _context.ConnectionId.Returns("conn-1");
    _clients.Group(Arg.Any<string>()).Returns(_groupClient);

    _hub = new VoyagerHub(_mediator)
    {
      Context = _context,
      Groups = _groups,
      Clients = _clients
    };
  }

  private void AuthenticateAs(Guid driverId)
  {
    var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, driverId.ToString())]);
    _context.User.Returns(new ClaimsPrincipal(identity));
  }

  [Fact]
  public async Task JoinRideGroup_AddsConnectionToGroup()
  {
    // Arrange
    var rideId = Guid.NewGuid().ToString();

    // Act
    await _hub.JoinRideGroup(rideId);

    // Assert
    await _groups.Received(1).AddToGroupAsync("conn-1", $"ride_{rideId}", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task LeaveRideGroup_RemovesConnectionFromGroup()
  {
    // Arrange
    var rideId = Guid.NewGuid().ToString();

    // Act
    await _hub.LeaveRideGroup(rideId);

    // Assert
    await _groups.Received(1).RemoveFromGroupAsync("conn-1", $"ride_{rideId}", Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UpdateDriverLocation_Throws_WhenUserNotAuthenticated()
  {
    // Arrange
    _context.User.Returns((ClaimsPrincipal?)null);
    var act = () => _hub.UpdateDriverLocation(new Point(0, 0));

    // Act & Assert
    var ex = await Assert.ThrowsAsync<HubException>(act);
    Assert.Equal("user_not_authenticated", ex.Message);
  }

  [Fact]
  public async Task UpdateDriverLocation_OnlyUpdatesLocation_WhenNoActiveRide()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    AuthenticateAs(driverId);
    _mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>()).Returns((ActiveRideResponse?)null);

    // Act
    await _hub.UpdateDriverLocation(new Point(1, 1));

    // Assert
    await _mediator.Received(1).Send(Arg.Is<UpdateLocation>(c => c.Id == driverId), Arg.Any<CancellationToken>());
    await _groupClient.DidNotReceive().SendToRiderNewDriverLocation(Arg.Any<Point>());
  }

  [Fact]
  public async Task UpdateDriverLocation_NotifiesRiderAndArrival_WhenCloseToPickup()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    var pickup = new Point(10, 10);
    var newLocation = new Point(10, 10); // same point => distance 0 => arrival fires
    AuthenticateAs(driverId);
    _mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>())
      .Returns(new ActiveRideResponse { Id = rideId, PickupLocation = pickup });
    _mediator.Send(Arg.Any<GetRideETA>(), Arg.Any<CancellationToken>())
      .Returns(new ETAResponse { EstimatedArrivalMinutes = 5, DistanceKm = 1.2 });

    // Act
    await _hub.UpdateDriverLocation(newLocation);

    // Assert
    await _groupClient.Received(1).SendToRiderNewDriverLocation(newLocation);
    await _groupClient.Received(1).SendToRiderNewETA(Arg.Is<ETAResponse>(e => e.EstimatedArrivalMinutes == 5));
    await _groupClient.Received(1).SendToRiderDriverArrival(rideId);
  }

  [Fact]
  public async Task UpdateDriverLocation_DoesNotNotifyArrival_WhenFarFromPickup()
  {
    // Arrange
    var driverId = Guid.NewGuid();
    var rideId = Guid.NewGuid();
    AuthenticateAs(driverId);
    _mediator.Send(Arg.Any<GetActiveRide>(), Arg.Any<CancellationToken>())
      .Returns(new ActiveRideResponse { Id = rideId, PickupLocation = new Point(0, 0) });
    _mediator.Send(Arg.Any<GetRideETA>(), Arg.Any<CancellationToken>())
      .Returns(new ETAResponse());

    // Act
    await _hub.UpdateDriverLocation(new Point(10000, 10000));

    // Assert
    await _groupClient.DidNotReceive().SendToRiderDriverArrival(Arg.Any<Guid>());
  }
}
