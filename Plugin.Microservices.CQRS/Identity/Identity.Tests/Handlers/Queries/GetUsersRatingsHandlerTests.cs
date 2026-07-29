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
    context.Users.Add(new VoyagerUser { Id = userId, Ratings = 4.5 });
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
}
