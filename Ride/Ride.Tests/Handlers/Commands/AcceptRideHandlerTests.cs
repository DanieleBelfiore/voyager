using FluentAssertions;
using Hub.API;
using Hub.Core.Interfaces;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using Ride.Core.CQRS.Commands;
using Ride.Core.Enums;
using Ride.Handlers.CQRS.Commands;
using Xunit;

namespace Ride.Tests.Handlers.Commands
{
  public class AcceptRideHandlerTests
  {
    private readonly IMediator _mediator;
    private readonly TestApplicationDbContext _context;
    private readonly IVoyagerShareClient _clientProxy;

    public AcceptRideHandlerTests()
    {
      _context = TestBase.CreateTestDbContext();

      _clientProxy = Substitute.For<IVoyagerShareClient>();
      var clientsProxy = Substitute.For<IHubClients<IVoyagerShareClient>>();
      var hubContext = Substitute.For<IHubContext<VoyagerHub, IVoyagerShareClient>>();
      clientsProxy.Group(Arg.Any<string>()).Returns(_clientProxy);
      hubContext.Clients.Returns(clientsProxy);

      var mediatorMock = Substitute.For<IMediator>();
      _mediator = mediatorMock;

      mediatorMock.Send(Arg.Any<AcceptRide>(), Arg.Any<CancellationToken>())
                  .Returns(c => new AcceptRideHandler(_context, hubContext)
                  .Handle(c.Arg<AcceptRide>(), c.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task AcceptRideTestFact()
    {
      // Arrange
      var rideId = Guid.NewGuid();
      var driverId = Guid.NewGuid();

      _context.Rides.Add(new Ride.Handlers.Models.Ride
      {
        Id = rideId
      });

      await _context.SaveChangesAsync();

      var request = new AcceptRide
      {
        RideId = rideId,
        DriverId = driverId
      };

      // Act
      await _mediator.Send(request);

      // Assert
      var result = await _context.Rides.FindAsync(rideId);

      result.Should().NotBeNull();
      result!.Status.Should().Be(RideStatus.DriverAssigned);
      result.DriverId.Should().Be(driverId);

      await _clientProxy.Received(1).SendToRiderRideAccepted(Arg.Is<Guid>(id => id == rideId));
    }
  }
}
