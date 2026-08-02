using Identity.Core.Ports.Secondary;
using Identity.Core.UseCases;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Identity.Tests.UseCases;

/// <summary>
/// The average is folded in by a single atomic statement behind IUserRepository.ApplyRatingAsync
/// — two ratings landing together used to read the same RatingsCount and silently drop one.
/// </summary>
public class UpdateUserRatingUseCaseTests
{
  private readonly IUserRepository _repository = Substitute.For<IUserRepository>();

  [Fact]
  public async Task UpdateUserRating_ShouldReturnTheRecomputedAverage()
  {
    // Arrange
    var userId = Guid.NewGuid();
    _repository.ApplyRatingAsync(userId, 5, Arg.Any<CancellationToken>()).Returns(4.5);

    // Act
    var result = await new UpdateUserRatingUseCase(_repository).Handle(new UpdateUserRating { UserId = userId, Rating = 5 }, CancellationToken.None);

    // Assert
    Assert.Equal(4.5, result);
  }

  [Fact]
  public async Task UpdateUserRating_ShouldNotLoadAndMutateTheEntity()
  {
    // Arrange — the load-mutate-save path is what lost concurrent ratings
    var userId = Guid.NewGuid();
    _repository.ApplyRatingAsync(userId, 5, Arg.Any<CancellationToken>()).Returns(5d);

    // Act
    await new UpdateUserRatingUseCase(_repository).Handle(new UpdateUserRating { UserId = userId, Rating = 5 }, CancellationToken.None);

    // Assert
    await _repository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task UpdateUserRating_ShouldThrow_WhenUserNotFound()
  {
    // Arrange
    _repository.ApplyRatingAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((double?)null);

    // Act
    var act = () => new UpdateUserRatingUseCase(_repository).Handle(new UpdateUserRating { UserId = Guid.NewGuid(), Rating = 5 }, CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(act);
  }
}
