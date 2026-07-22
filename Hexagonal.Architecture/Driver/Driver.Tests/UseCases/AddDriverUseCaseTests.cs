using Driver.Core.Ports.Secondary;
using Driver.Core.UseCases;
using DriverEntity = Driver.Core.Domain.Driver;
using FluentAssertions;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;

namespace Driver.Tests.UseCases;

public class AddDriverUseCaseTests
{
  private readonly IDriverRepository _repository;
  private readonly AddDriverUseCase _useCase;

  public AddDriverUseCaseTests()
  {
    _repository = Substitute.For<IDriverRepository>();
    _useCase = new AddDriverUseCase(_repository);
  }

  [Fact]
  public async Task AddDriver_ShouldPersist_WhenNew()
  {
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((DriverEntity?)null);

    await _useCase.Handle(new AddDriver { DriverId = id }, CancellationToken.None);

    _repository.Received(1).Add(Arg.Is<DriverEntity>(d => d.Id == id));
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task AddDriver_ShouldBeIdempotent_WhenDriverAlreadyExists()
  {
    var id = Guid.NewGuid();
    _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(new DriverEntity(id));

    await _useCase.Handle(new AddDriver { DriverId = id }, CancellationToken.None);

    _repository.DidNotReceive().Add(Arg.Any<DriverEntity>());
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
