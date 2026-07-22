using Identity.Application.CQRS.Commands;
using Identity.Application.Ports;
using UserEntity = Identity.Domain.Entities.User;
using FluentAssertions;
using NSubstitute;
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

  [Fact]
  public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
  {
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "x", false));

    var act = () => _handler.Handle(ValidCommand(), CancellationToken.None);

    (await act.Should().ThrowAsync<Exception>()).WithMessage("already_exist");
  }

  [Fact]
  public async Task Register_ShouldThrow_WhenPasswordsDoNotMatch()
  {
    var command = ValidCommand();
    command.ConfirmPassword = "different";

    var act = () => _handler.Handle(command, CancellationToken.None);

    (await act.Should().ThrowAsync<Exception>()).WithMessage("confirm_password_not_matching");
  }
}
