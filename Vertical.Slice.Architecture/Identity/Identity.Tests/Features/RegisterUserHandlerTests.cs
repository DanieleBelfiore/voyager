using Identity.Api.Features.RegisterUser;
using Identity.Api.Persistence;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Voyager.Contracts.Driver;
using Xunit;
using UserEntity = Identity.Api.Entities.User;

namespace Identity.Tests.Features;

public class RegisterUserHandlerTests
{
  private static IdentityDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new IdentityDbContext(options);
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
    await using var db = NewContext();
    var handler = new RegisterUserHandler(db, new PasswordHasher<object>(), Substitute.For<IMediator>());

    await handler.Handle(ValidCommand(), CancellationToken.None);

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == "ada@example.com");
    Assert.NotNull(user);
    Assert.False(string.IsNullOrEmpty(user!.PasswordHash));
  }

  [Fact]
  public async Task Register_ShouldNotifyDriverRegistration_WhenIsDriver()
  {
    await using var db = NewContext();
    var mediator = Substitute.For<IMediator>();
    var handler = new RegisterUserHandler(db, new PasswordHasher<object>(), mediator);

    await handler.Handle(ValidCommand(isDriver: true), CancellationToken.None);

    await mediator.Received(1).Send(Arg.Any<AddDriver>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Register_ShouldThrow_WhenEmailAlreadyExists()
  {
    await using var db = NewContext();
    db.Users.Add(new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "x", false));
    await db.SaveChangesAsync();

    var handler = new RegisterUserHandler(db, new PasswordHasher<object>(), Substitute.For<IMediator>());

    var act = () => handler.Handle(ValidCommand(), CancellationToken.None);

    var ex = await Assert.ThrowsAsync<Exception>(act);
    Assert.Equal("already_exist", ex.Message);
  }
}
