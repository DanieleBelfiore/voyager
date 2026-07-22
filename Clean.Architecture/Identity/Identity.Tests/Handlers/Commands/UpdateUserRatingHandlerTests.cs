using Identity.Application.CQRS.Commands;
using Identity.Application.Ports;
using UserEntity = Identity.Domain.Entities.User;
using FluentAssertions;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Identity.Tests.Handlers.Commands;

public class UpdateUserRatingHandlerTests
{
  [Fact]
  public async Task UpdateUserRating_ShouldRecomputeAndPersistRating()
  {
    var repository = Substitute.For<IUserRepository>();
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "hash", true);
    repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

    var handler = new UpdateUserRatingHandler(repository);

    var result = await handler.Handle(new UpdateUserRating { UserId = user.Id, Rating = 5, Rides = 1 }, CancellationToken.None);

    result.Should().Be(5);
    user.Ratings.Should().Be(5);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
