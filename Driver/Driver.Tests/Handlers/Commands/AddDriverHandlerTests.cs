using Driver.Core.CQRS.Commands;
using Driver.Core.Enums;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Driver.Tests.Handlers.Commands
{
  public class AddDriverHandlerTests
  {
    private readonly IMediator _mediator;
    private readonly TestApplicationDbContext _context;

    public AddDriverHandlerTests()
    {
      var (context, _) = TestBase.CreateTestServices();
      _context = context;

      var mediatorMock = Substitute.For<IMediator>();

      _mediator = mediatorMock;

      mediatorMock.Send(Arg.Any<AddDriver>(), Arg.Any<CancellationToken>())
                  .Returns(c => new AddDriverHandler(_context)
                  .Handle(c.Arg<AddDriver>(), c.Arg<CancellationToken>()));
    }

    [Fact]
    public async Task AddDriverTestFact()
    {
      // Arrange
      var id = Guid.NewGuid();

      _context.Drivers.Add(new Driver.Handlers.Models.Driver
      {
        Id = id
      });

      await _context.SaveChangesAsync();

      var request = new AddDriver
      {
        DriverId = id
      };

      // Act
      await _mediator.Send(request);

      // Assert
      var result = await _context.Drivers.FindAsync(id);

      result.Should().NotBeNull();
      result!.Status.Should().Be(DriverStatus.Available);
      result.Id.Should().Be(id);
    }
  }
}
