using Identity.Module.Features.GetUsersRatings;
using Identity.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Module.Entities.User;

namespace Identity.Tests.Features;

public class GetUsersRatingsHandlerTests
{
  [Fact]
  public async Task Handle_ReturnsRatingsForRequestedUsers()
  {
    // Arrange
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var db = new IdentityDbContext(options);
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "hash", true);
    db.Users.Add(user);
    // Ratings/RatingsCount have private setters and are only ever written by ApplyRating's
    // ExecuteUpdate, which the InMemory provider doesn't support — set them through the change
    // tracker instead of widening the entity's surface for a test.
    db.Entry(user).Property(u => u.Ratings).CurrentValue = 4.5;
    db.Entry(user).Property(u => u.RatingsCount).CurrentValue = 2;
    await db.SaveChangesAsync();
    var handler = new GetUsersRatingsHandler(db);

    // Act
    var result = await handler.Handle(new Voyager.Contracts.Identity.GetUsersRatings { UserIds = [user.Id] }, CancellationToken.None);

    // Assert
    Assert.Equal(4.5, result[user.Id]);
  }

  [Fact]
  public async Task Handle_OmitsUser_WhenNeverRated()
  {
    // Arrange: Ratings defaults to 0.0, so returning this user would report the worst possible
    // score for someone who simply has no ratings yet — and would mask SearchBestDriver's
    // "unrated defaults to the midpoint" fallback, which keys off the id being absent.
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    await using var db = new IdentityDbContext(options);
    var unrated = new UserEntity(Guid.NewGuid(), "new@example.com", "New", "Driver", null, "hash", true);
    var rated = new UserEntity(Guid.NewGuid(), "vet@example.com", "Vet", "Driver", null, "hash", true);
    db.Users.AddRange(unrated, rated);
    db.Entry(rated).Property(u => u.Ratings).CurrentValue = 3.0;
    db.Entry(rated).Property(u => u.RatingsCount).CurrentValue = 1;
    await db.SaveChangesAsync();
    var handler = new GetUsersRatingsHandler(db);

    // Act
    var result = await handler.Handle(new Voyager.Contracts.Identity.GetUsersRatings { UserIds = [unrated.Id, rated.Id] }, CancellationToken.None);

    // Assert
    Assert.False(result.ContainsKey(unrated.Id));
    Assert.Equal(3.0, result[rated.Id]);
  }
}
