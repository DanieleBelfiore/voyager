using Identity.Module.Features.AuthenticateUser;
using Identity.Module.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Module.Entities.User;

namespace Identity.Tests.Features;

public class AuthenticateUserHandlerTests
{
  private static IdentityDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new IdentityDbContext(options);
  }

  [Fact]
  public async Task Handle_ReturnsSuccess_WhenCredentialsValid()
  {
    // Arrange
    await using var db = NewContext();
    var hasher = new PasswordHasher<object>();
    var hash = hasher.HashPassword(null!, "Str0ng!Pass");
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, hash, true);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    var handler = new AuthenticateUserHandler(db, hasher);

    // Act
    var result = await handler.Handle(new AuthenticateUser { Username = "ada@example.com", Password = "Str0ng!Pass" }, CancellationToken.None);

    // Assert
    Assert.True(result.Succeeded);
    Assert.Equal(user.Id, result.UserId);
    Assert.Equal(user.Email, result.Email);
    Assert.True(result.IsDriver);
  }

  [Fact]
  public async Task Handle_ReturnsFailure_WhenUserNotFound()
  {
    // Arrange
    await using var db = NewContext();
    var handler = new AuthenticateUserHandler(db, new PasswordHasher<object>());

    // Act
    var result = await handler.Handle(new AuthenticateUser { Username = "missing@example.com", Password = "x" }, CancellationToken.None);

    // Assert
    Assert.False(result.Succeeded);
  }

  [Fact]
  public async Task Handle_ReturnsFailure_WhenPasswordInvalid()
  {
    // Arrange
    await using var db = NewContext();
    var hasher = new PasswordHasher<object>();
    var hash = hasher.HashPassword(null!, "Str0ng!Pass");
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, hash, false);
    db.Users.Add(user);
    await db.SaveChangesAsync();
    var handler = new AuthenticateUserHandler(db, hasher);

    // Act
    var result = await handler.Handle(new AuthenticateUser { Username = "ada@example.com", Password = "wrong" }, CancellationToken.None);

    // Assert
    Assert.False(result.Succeeded);
  }
}
