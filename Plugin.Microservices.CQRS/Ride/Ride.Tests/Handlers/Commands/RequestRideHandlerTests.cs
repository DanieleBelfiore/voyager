using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class RequestRideHandlerTests
{
  private static readonly Point Pickup = new(0, 0);
  private static readonly Point Dropoff = new(1, 1);
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IVoyagerShareClient _clientProxy;

  public RequestRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    _clientProxy = Substitute.For<IVoyagerShareClient>();
    var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
    hubContext.Clients.Returns(clientsProxy);

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<RequestRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new RequestRideHandler(_context, new RideMapper(), hubContext)
        .Handle(c.Arg<RequestRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CreatesRideAndPushesToHub_WhenNoInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    var driverId = Guid.NewGuid();

    // Act
    var result = await _mediator.Send(new RequestRide { UserId = userId, DriverId = driverId, PickupLocation = Pickup, DropoffLocation = Dropoff });

    // Assert
    Assert.Equal(userId, result.UserId);
    Assert.Equal(driverId, result.DriverId);
    Assert.Equal(RideStatus.Requested, result.Status);
    Assert.Single(_context.Rides);
    await _clientProxy.Received(1).SendToDriverNewRideRequest(result.Id);
  }

  [Fact]
  public async Task Handle_Throws_WhenUserHasInFlightRide()
  {
    // Arrange
    var userId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { UserId = userId, Status = RideStatus.Requested });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new RequestRide { UserId = userId, DriverId = Guid.NewGuid(), PickupLocation = Pickup, DropoffLocation = Dropoff });

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(act);
  }
}
