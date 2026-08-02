using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using Identity.Handlers.CQRS.Commands;
using Identity.Handlers.Models;
using MediatR;
using NSubstitute;
using Xunit;

namespace Identity.Tests.Handlers.Commands;

public class UpdateUserRatingHandlerTests
{
  private readonly IMediator _mediator;
  private readonly TestApplicationDbContext _context;

  public UpdateUserRatingHandlerTests()
  {
    _context = TestBase.CreateTestDbContext();

    var mediatorMock = Substitute.For<IMediator>();
    _mediator = mediatorMock;

    mediatorMock.Send(Arg.Any<UpdateUserRating>(), Arg.Any<CancellationToken>())
      .Returns(c => new UpdateUserRatingHandler(_context)
        .Handle(c.Arg<UpdateUserRating>(), c.Arg<CancellationToken>()));
  }

  [Fact]
  public async Task Handle_RecomputesAndPersistsRating()
  {
    // Arrange
    var userId = Guid.NewGuid();
    _context.Users.Add(new VoyagerUser { Id = userId, Ratings = 0 });
    await _context.SaveChangesAsync();

    // Act
    var result = await _mediator.Send(new UpdateUserRating { UserId = userId, Rating = 5 });

    // Assert
    Assert.Equal(5, result);
    var user = await _context.Users.FindAsync(userId);
    Assert.Equal(5, user!.Ratings);
  }

  [Fact]
  public async Task Handle_Throws_WhenUserNotFound()
  {
    // Arrange
    var act = () => _mediator.Send(new UpdateUserRating { UserId = Guid.NewGuid(), Rating = 5 });

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("user_not_found", ex.Message);
  }
}
