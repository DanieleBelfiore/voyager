using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NetTopologySuite.Geometries;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands;

public class CompleteRideHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;
  private readonly IVoyagerShareClient _clientProxy;

  public CompleteRideHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    _clientProxy = Substitute.For<IVoyagerShareClient>();
    var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
    var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
    clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
    hubContext.Clients.Returns(clientsProxy);

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<CompleteRide>(), Arg.Any<CancellationToken>())
      .Returns(c => new CompleteRideHandler(_context, hubContext)
        .Handle(c.Arg<CompleteRide>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_CompletesRideAndPushesToHub_WhenRideExists()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.InProgress });
    await _context.SaveChangesAsync();
    var dropoff = new Point(3, 3);

    // Act
    await _mediator.Send(new CompleteRide { Id = rideId, CallerId = driverId, Location = dropoff, Price = 42.5 });

    // Assert
    var ride = await _context.Rides.FindAsync(rideId);
    Assert.Equal(RideStatus.Completed, ride!.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.Equal(42.5, ride.Price);
    await _clientProxy.Received(1).SendToRiderRideCompleted(rideId);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new CompleteRide { Id = Guid.NewGuid(), Location = new Point(0, 0), Price = 0 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenCallerIsNotAssignedDriver()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.InProgress });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CompleteRide { Id = rideId, CallerId = Guid.NewGuid(), Location = new Point(0, 0), Price = 0 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    Assert.Equal("not_ride_participant", ex.Message);
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotInProgress()
  {
    // Arrange
    var rideId = Guid.NewGuid();
    var driverId = Guid.NewGuid();
    _context.Rides.Add(new Ride.Handlers.Models.Ride { Id = rideId, DriverId = driverId, Status = RideStatus.DriverAssigned });
    await _context.SaveChangesAsync();
    var act = () => _mediator.Send(new CompleteRide { Id = rideId, CallerId = driverId, Location = new Point(0, 0), Price = 0 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("operation_not_permitted", ex.Message);
  }
}
