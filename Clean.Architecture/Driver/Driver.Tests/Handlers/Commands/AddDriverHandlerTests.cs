using Driver.Application.CQRS.Commands;
using Driver.Application.Ports;
using DriverEntity = Driver.Domain.Entities.Driver;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.Handlers.Commands;

public class AddDriverHandlerTests
{
  private readonly IDriverRepository _repository;
  private readonly AddDriverHandler _handler;

  public AddDriverHandlerTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _handler = new AddDriverHandler(_repository);
  }

  [Fact]
  public async Task AddDriverTestFact()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);

    // Act
    await _handler.Handle(new AddDriver { DriverId = id }, CancellationToken.None);

    // Assert
    _repository.Received(1).Add(Arg.Is<DriverEntity>(d => d.Id == id && d.Status == Driver.Domain.Enums.DriverStatus.Available));
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AddDriver_ShouldBeIdempotent_WhenDriverAlreadyExists()
  {
    // Arrange
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    // Act
    await _handler.Handle(new AddDriver { DriverId = id }, CancellationToken.None);

    // Assert
    _repository.DidNotReceive().Add(Arg.Any<DriverEntity>());
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
