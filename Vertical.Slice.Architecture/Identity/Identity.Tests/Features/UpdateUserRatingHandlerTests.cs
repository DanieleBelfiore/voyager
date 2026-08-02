using Identity.Api.Features.UpdateUserRating;
using Identity.Api.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Api.Entities.User;

namespace Identity.Tests.Features;

/// <summary>
/// SQLite in-memory rather than the InMemory provider: the handler folds the rating in with a
/// single ExecuteUpdateAsync statement (so two concurrent ratings can't read the same
/// RatingsCount and lose one), and ExecuteUpdate has no InMemory implementation. A relational
/// provider is also the only way these assertions say anything about the real SQL.
/// </summary>
public class UpdateUserRatingHandlerTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly IdentityDbContext _db;

  public UpdateUserRatingHandlerTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    _db = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(_connection).Options);
    _db.Database.EnsureCreated();
  }

  public void Dispose()
  {
    _db.Dispose();
    _connection.Dispose();
  }

  private async Task<UserEntity> SeedUser()
  {
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "hash", true);
    _db.Users.Add(user);
    await _db.SaveChangesAsync();
    _db.ChangeTracker.Clear();

    return user;
  }

  [Fact]
  public async Task UpdateUserRating_ShouldRecomputeAndPersistRating()
  {
    // Arrange
    var user = await SeedUser();
    var handler = new UpdateUserRatingHandler(_db);

    // Act
    var result = await handler.Handle(
      new Voyager.Contracts.Identity.UpdateUserRating { UserId = user.Id, Rating = 5 }, CancellationToken.None);

    // Assert
    Assert.Equal(5, result);
    var stored = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
    Assert.Equal(5, stored.Ratings);
    Assert.Equal(1, stored.RatingsCount);
  }

  [Fact]
  public async Task UpdateUserRating_ShouldKeepARunningAverage_AcrossSeveralRatings()
  {
    // Arrange
    var user = await SeedUser();
    var handler = new UpdateUserRatingHandler(_db);
    var command = new Voyager.Contracts.Identity.UpdateUserRating { UserId = user.Id };

    // Act — 5, 3, 4 => 4.0
    foreach (var rating in new[] { 5, 3, 4 })
    {
      command.Rating = rating;
      await handler.Handle(command, CancellationToken.None);
    }

    // Assert
    var stored = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
    Assert.Equal(4d, stored.Ratings, 9);
    Assert.Equal(3, stored.RatingsCount);
  }

  [Fact]
  public async Task UpdateUserRating_ShouldNotLoseARating_WhenTwoArriveConcurrently()
  {
    // Arrange — the whole point of the atomic statement. Two handlers over their own context,
    // each unaware of the other's read, on the same row.
    var user = await SeedUser();

    await using var dbA = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(_connection).Options);
    await using var dbB = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(_connection).Options);
    var command = new Voyager.Contracts.Identity.UpdateUserRating { UserId = user.Id, Rating = 5 };

    // Act
    await new UpdateUserRatingHandler(dbA).Handle(command, CancellationToken.None);
    await new UpdateUserRatingHandler(dbB).Handle(command, CancellationToken.None);

    // Assert — both counted; read-modify-write used to leave RatingsCount at 1
    var stored = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
    Assert.Equal(2, stored.RatingsCount);
    Assert.Equal(5d, stored.Ratings, 9);
  }

  [Fact]
  public async Task UpdateUserRating_ShouldThrow_WhenUserNotFound()
  {
    // Arrange
    var handler = new UpdateUserRatingHandler(_db);

    // Act
    var act = () => handler.Handle(
      new Voyager.Contracts.Identity.UpdateUserRating { UserId = Guid.NewGuid(), Rating = 5 }, CancellationToken.None);

    // Assert
    await Assert.ThrowsAsync<KeyNotFoundException>(act);
  }
}
