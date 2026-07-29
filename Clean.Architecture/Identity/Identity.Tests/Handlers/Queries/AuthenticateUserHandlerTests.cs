using Identity.Application.CQRS.Queries;
using Identity.Application.Ports;
using UserEntity = Identity.Domain.Entities.User;
using NSubstitute;
using Xunit;

namespace Identity.Tests.Handlers.Queries;

public class AuthenticateUserHandlerTests
{
  private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
  private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
  private readonly AuthenticateUserHandler _handler;

  public AuthenticateUserHandlerTests()
  {
    _handler = new AuthenticateUserHandler(_repository, _passwordHasher);
  }

  [Fact]
  public async Task Handle_ReturnsSuccess_WhenCredentialsValid()
  {
    // Arrange
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", "123", "hashed", isDriver: true);
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.Verify("hashed", "Str0ng!Pass").Returns(true);

    // Act
    var result = await _handler.Handle(new AuthenticateUser { Username = "ada@example.com", Password = "Str0ng!Pass" }, CancellationToken.None);

    // Assert
    Assert.True(result.Succeeded);
    Assert.Equal(user.Id, result.UserId);
    Assert.Equal(user.Email, result.Email);
    Assert.True(result.IsDriver);
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsFailure_WhenUserNotFound()
  {
    // Arrange
    _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

    // Act
    var result = await _handler.Handle(new AuthenticateUser { Username = "missing@example.com", Password = "x" }, CancellationToken.None);

    // Assert
    Assert.False(result.Succeeded);
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsFailure_WhenPasswordInvalid()
  {
    // Arrange
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", "123", "hashed", isDriver: false);
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.Verify("hashed", "wrong").Returns(false);

    // Act
    var result = await _handler.Handle(new AuthenticateUser { Username = "ada@example.com", Password = "wrong" }, CancellationToken.None);

    // Assert
    Assert.False(result.Succeeded);
    await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
