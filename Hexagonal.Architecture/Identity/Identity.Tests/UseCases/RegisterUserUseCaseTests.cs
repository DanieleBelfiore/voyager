using Identity.Core.Ports.Secondary;
using Identity.Core.Ports.Primary;
using Identity.Core.UseCases;
using UserEntity = Identity.Core.Domain.User;
using NSubstitute;
using Xunit;

namespace Identity.Tests.UseCases;

public class RegisterUserUseCaseTests
{
  private readonly IUserRepository _repository;
  private readonly IPasswordHasher _passwordHasher;
  private readonly IDriverRegistration _driverRegistration;
  private readonly RegisterUserUseCase _useCase;

  public RegisterUserUseCaseTests()
  {
    _repository = Substitute.For<IUserRepository>();
    _passwordHasher = Substitute.For<IPasswordHasher>();
    _driverRegistration = Substitute.For<IDriverRegistration>();
    _useCase = new RegisterUserUseCase(_repository, _passwordHasher, _driverRegistration);

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

    await _useCase.Handle(ValidCommand(), CancellationToken.None);

    _repository.Received(1).Add(Arg.Is<UserEntity>(u => u.Email == "ada@example.com" && u.PasswordHash == "hashed:Str0ng!Pass"));
    await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Register_ShouldNotifyDriverRegistration_WhenIsDriver()
  {
    _repository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((UserEntity?)null);

    await _useCase.Handle(ValidCommand(isDriver: true), CancellationToken.None);

    await _driverRegistration.Received(1).RegisterAsDriverAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
  {
    _repository.GetByEmailAsync("ada@example.com", Arg.Any<CancellationToken>()).Returns(new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "x", false));

    var act = () => _useCase.Handle(ValidCommand(), CancellationToken.None);

    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("already_exist", ex.Message);
  }
}
