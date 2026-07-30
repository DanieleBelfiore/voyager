using NetTopologySuite.Geometries;
using Ride.Core.Ports.Primary;
using Ride.Core.Ports.Secondary;
using Ride.Core.UseCases;
using RideEntity = Ride.Core.Domain.Ride;
using NSubstitute;
using Xunit;

namespace Ride.Tests.UseCases;

public class CompleteRideUseCaseTests
{
  private static readonly Point SomePoint = new(0, 0);
  private readonly IRideRepository _repository = Substitute.For<IRideRepository>();
  private readonly IRideEventPublisher _events = Substitute.For<IRideEventPublisher>();
  private readonly CompleteRideUseCase _useCase;

  public CompleteRideUseCaseTests()
  {
    _useCase = new CompleteRideUseCase(_repository, _events);
  }

  [Fact]
  public async Task Handle_CompletesRideAndPublishesEvent_WhenRideExists()
  {
    // Arrange
    var ride = new RideEntity(Guid.NewGuid(), Guid.NewGuid(), SomePoint, SomePoint);
    ride.Accept(ride.DriverId);
    ride.Start(SomePoint);
    var dropoff = new Point(3, 3);
    _repository.GetByIdAsync(ride.Id, Arg.Any<CancellationToken>()).Returns(ride);

    // Act
    await _useCase.Handle(new CompleteRide { Id = ride.Id, Location = dropoff, Price = 42.5, CallerId = ride.DriverId }, CancellationToken.None);

    // Assert
    Assert.Equal(Ride.Core.Domain.RideStatus.Completed, ride.Status);
    Assert.Equal(dropoff, ride.DropoffLocation);
    Assert.Equal(42.5, ride.Price);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    await _events.Received(1).RideCompletedAsync(ride.Id, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_Throws_WhenRideNotFound()
  {
    // Arrange
    _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((RideEntity?)null);
    var act = () => _useCase.Handle(new CompleteRide { Id = Guid.NewGuid(), Location = SomePoint, Price = 0 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("no_ride_found", ex.Message);
  }
}
