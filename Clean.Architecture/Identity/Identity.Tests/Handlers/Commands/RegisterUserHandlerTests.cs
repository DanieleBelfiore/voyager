using Identity.Application.CQRS.Commands;
using Identity.Application.Ports;
using UserEntity = Identity.Domain.Entities.User;
using NSubstitute;
using Voyager.Errors;
using Xunit;

namespace Identity.Tests.Handlers.Commands;

public class RegisterUserHandlerTests
{
  private readonly IUserRepository _repository;
  private readonly IPasswordHasher _passwordHasher;
  private readonly IDriverRegistration _driverRegistration;
  private readonly RegisterUserHandler _handler;

  public RegisterUserHandlerTests()
  {
    _repository = Substitute.For<IUserRepository>();
    _passwordHasher = Substitute.For<IPasswordHasher>();
    _driverRegistration = Substitute.For<IDriverRegistration>();
    _handler = new RegisterUserHandler(_repository, _passwordHasher, _driverRegistration);

    _passwordHasher.Hash(Arg.Any<string>()).Returns(c => $"hashed:{c.Arg<string>()}");
  }

  private static RegisterUser ValidCommand(bool isDriver = false) => new()
  {
    FirstName = "Ada",
    LastName = "Lovelace",
    Email = "ada@example.com",
    Password = "Str0ng!Pass",
    ConfirmPassword = "Str0ng!Pass",
    IsDriver = isDriver
  };

  [Fact]
  public async Task Register_ShouldPersistUser_WhenValid()
  {
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

    await _handler.Handle(ValidCommand(), CancellationToken.None);

    _repository.Received(1).Add(Arg.Is<UserEntity>(u => u.Email == "ada@example.com" && u.PasswordHash == "hashed:Str0ng!Pass"));
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Register_ShouldNotifyDriverRegistration_WhenIsDriver()
  {
    _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

    await _handler.Handle(ValidCommand(isDriver: true), CancellationToken.None);

    await _driverRegistration.Received(1).RegisterAsDriverAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
  }

  // The exception types are asserted, not just the messages: the controller decides whether a
  // message is safe to return to the caller by type, so a rule violation degrading back to a
  // bare Exception would silently be reported as an opaque "registration_failed".
  [Fact]
  public async Task Register_ShouldThrowConflict_WhenEmailAlreadyExists()
  {
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "x", false));

    var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

    var ex = await Assert.ThrowsAsync<ConflictException>(act);
    Assert.Equal("already_exist", ex.Message);
  }

  [Fact]
  public async Task Register_ShouldThrowInvalidInput_WhenPasswordsDoNotMatch()
  {
    var command = ValidCommand();
    command.ConfirmPassword = "different";

    var act = () => _handler.Handle(command, CancellationToken.None);

    var ex = await Assert.ThrowsAsync<InvalidInputException>(act);
    Assert.Equal("confirm_password_not_matching", ex.Message);
  }

  [Fact]
  public async Task Register_ShouldRemoveUser_WhenDriverRegistrationFails()
  {
    // The user and the Driver row live in different services, so there is no transaction across
    // them. Leaving the user behind made the failure permanent: the caller was told registration
    // failed, but retrying hit "already_exist" and the driver was never matchable.
    _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserEntity?)null);
    _driverRegistration.RegisterAsDriverAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
      .Returns<Task>(_ => throw new TimeoutException("driver service unreachable"));

    var act = () => _handler.Handle(ValidCommand(isDriver: true), CancellationToken.None);

    await Assert.ThrowsAsync<TimeoutException>(act);
    _repository.Received(1).Remove(Arg.Is<UserEntity>(u => u.Email == "ada@example.com"));
    await _repository.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Register_ShouldNotRemoveUser_WhenDriverRegistrationSucceeds()
  {
    _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

    await _handler.Handle(ValidCommand(isDriver: true), CancellationToken.None);

    _repository.DidNotReceive().Remove(Arg.Any<UserEntity>());
  }
}
