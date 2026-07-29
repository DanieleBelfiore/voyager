using Identity.Application.CQRS.Queries;
using Identity.Application.Ports;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Identity.Tests.Handlers.Queries;

public class GetUsersRatingsHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsRatingsFromRepository()
  {
    // Arrange
    var repository = Substitute.For<IUserRepository>();
    var userId = Guid.NewGuid();
    var expected = new Dictionary<Guid, double> { [userId] = 4.5 };
    repository.GetRatingsAsync(Arg.Is<List<Guid>>(l => l.Contains(userId)), Arg.Any<CancellationToken>()).Returns(expected);
    var handler = new GetUsersRatingsHandler(repository);

    // Act
    var result = await handler.Handle(new GetUsersRatings { UserIds = [userId] }, CancellationToken.None);

    // Assert
    Assert.Equal(expected, result);
  }
}
