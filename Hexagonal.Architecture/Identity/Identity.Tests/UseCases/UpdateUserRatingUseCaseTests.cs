using Identity.Core.Ports.Secondary;
using Identity.Core.UseCases;
using UserEntity = Identity.Core.Domain.User;
using FluentAssertions;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Identity.Tests.UseCases;

public class UpdateUserRatingUseCaseTests
{
  [Fact]
  public async Task UpdateUserRating_ShouldRecomputeAndPersistRating()
  {
    var repository = Substitute.For<IUserRepository>();
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "hash", true);
    repository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

    var useCase = new UpdateUserRatingUseCase(repository);

    var result = await useCase.Handle(new UpdateUserRating { UserId = user.Id, Rating = 5, Rides = 1 }, CancellationToken.None);

    result.Should().Be(5);
    user.Ratings.Should().Be(5);
    await repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
