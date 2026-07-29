using Identity.Core.Ports.Primary;
using Identity.Core.Ports.Secondary;
using Identity.Core.UseCases;
using NSubstitute;
using Voyager.Contracts.Identity;
using Xunit;

namespace Identity.Tests.UseCases;

public class GetUsersRatingsUseCaseTests
{
  [Fact]
  public async Task Handle_ReturnsRatingsFromRepository()
  {
    // Arrange
    var repository = Substitute.For<IUserRepository>();
    var userId = Guid.NewGuid();
    var expected = new Dictionary<Guid, double> { [userId] = 4.5 };
    repository.GetRatingsAsync(Arg.Is<List<Guid>>(l => l.Contains(userId)), Arg.Any<CancellationToken>()).Returns(expected);
    var useCase = new GetUsersRatingsUseCase(repository);

    // Act
    var result = await useCase.Handle(new GetUsersRatings { UserIds = [userId] }, CancellationToken.None);

    // Assert
    Assert.Equal(expected, result);
  }
}
