using Identity.Core.CQRS.Queries;
using Identity.Handlers.CQRS.Queries;
using Identity.Handlers.Models;
using MediatR;
using NSubstitute;
using Xunit;

namespace Identity.Tests.Handlers.Queries;

public class GetUsersRatingsHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsRatingsForRequestedUsers()
  {
    // Arrange
    var context = TestBase.CreateTestDbContext();
    var userId = Guid.NewGuid();
    context.Users.Add(new VoyagerUser { Id = userId, Ratings = 4.5, RatingsCount = 2 });
    await context.SaveChangesAsync();

    var mediatorMock = Substitute.For<IMediator>();
    mediatorMock.Send(Arg.Any<GetUsersRatings>(), Arg.Any<CancellationToken>())
      .Returns(c => new GetUsersRatingsHandler(context)
        .Handle(c.Arg<GetUsersRatings>(), c.Arg<CancellationToken>()));

    // Act
    var result = await mediatorMock.Send(new GetUsersRatings { UserIds = [userId] });

    // Assert
    Assert.Equal(4.5, result[userId]);
  }

  [Fact]
  public async Task Handle_OmitsUser_WhenNeverRated()
  {
    // Arrange: Ratings defaults to 0.0, so returning this user would report the worst possible
    // score for someone who simply has no ratings yet — and would mask the caller's own
    // "unrated defaults to the midpoint" fallback, which keys off the id being absent.
    var context = TestBase.CreateTestDbContext();
    var unratedId = Guid.NewGuid();
    var ratedId = Guid.NewGuid();
    context.Users.Add(new VoyagerUser { Id = unratedId, Ratings = 0, RatingsCount = 0 });
    context.Users.Add(new VoyagerUser { Id = ratedId, Ratings = 3.0, RatingsCount = 1 });
    await context.SaveChangesAsync();

    // Act
    var result = await new GetUsersRatingsHandler(context)
      .Handle(new GetUsersRatings { UserIds = [unratedId, ratedId] }, CancellationToken.None);

    // Assert
    Assert.False(result.ContainsKey(unratedId));
    Assert.Equal(3.0, result[ratedId]);
  }
}
