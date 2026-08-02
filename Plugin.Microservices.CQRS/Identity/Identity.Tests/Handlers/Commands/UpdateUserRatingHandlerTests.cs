using Common.Core.Exceptions;
using Identity.Core.CQRS.Commands;
using Identity.Handlers.CQRS.Commands;
using Identity.Handlers.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Identity.Tests.Handlers.Commands;

/// <summary>
/// SQLite in-memory rather than the InMemory provider: the handler folds the rating in with a
/// single ExecuteUpdateAsync statement (so two concurrent ratings can't read the same
/// RatingsCount and lose one), and ExecuteUpdate has no InMemory implementation. A relational
/// provider is also the only way these assertions say anything about the real SQL.
/// </summary>
public class UpdateUserRatingHandlerTests : IDisposable
{
  private readonly SqliteConnection _connection;
  private readonly TestApplicationDbContext _context;

  public UpdateUserRatingHandlerTests()
  {
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    _context = NewContext();
    _context.Database.EnsureCreated();
  }

  private TestApplicationDbContext NewContext() =>
    new(new DbContextOptionsBuilder<TestApplicationDbContext>().UseSqlite(_connection).Options);

  public void Dispose()
  {
    _context.Dispose();
    _connection.Dispose();
  }

  private async Task<Guid> SeedUser()
  {
    var userId = Guid.NewGuid();
    _context.Users.Add(new VoyagerUser { Id = userId, Ratings = 0 });
    await _context.SaveChangesAsync(CancellationToken.None);
    _context.ChangeTracker.Clear();

    return userId;
  }

  [Fact]
  public async Task Handle_RecomputesAndPersistsRating()
  {
    // Arrange
    var userId = await SeedUser();

    // Act
    var result = await new UpdateUserRatingHandler(_context)
      .Handle(new UpdateUserRating { UserId = userId, Rating = 5 }, CancellationToken.None);

    // Assert
    Assert.Equal(5, result);
    var user = await _context.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
    Assert.Equal(5, user.Ratings);
    Assert.Equal(1, user.RatingsCount);
  }

  [Fact]
  public async Task Handle_KeepsARunningAverage_AcrossSeveralRatings()
  {
    // Arrange
    var userId = await SeedUser();
    var handler = new UpdateUserRatingHandler(_context);

    // Act — 5, 3, 4 => 4.0
    foreach (var rating in new[] { 5, 3, 4 })
      await handler.Handle(new UpdateUserRating { UserId = userId, Rating = rating }, CancellationToken.None);

    // Assert
    var user = await _context.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
    Assert.Equal(4d, user.Ratings, 9);
    Assert.Equal(3, user.RatingsCount);
  }

  [Fact]
  public async Task Handle_DoesNotLoseARating_WhenTwoArriveConcurrently()
  {
    // Arrange — the whole point of the atomic statement. Two handlers over their own context,
    // each unaware of the other's read, on the same row.
    var userId = await SeedUser();
    await using var contextA = NewContext();
    await using var contextB = NewContext();

    // Act
    await new UpdateUserRatingHandler(contextA).Handle(new UpdateUserRating { UserId = userId, Rating = 5 }, CancellationToken.None);
    await new UpdateUserRatingHandler(contextB).Handle(new UpdateUserRating { UserId = userId, Rating = 5 }, CancellationToken.None);

    // Assert — both counted; read-modify-write used to leave RatingsCount at 1
    var user = await _context.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
    Assert.Equal(2, user.RatingsCount);
    Assert.Equal(5d, user.Ratings, 9);
  }

  [Fact]
  public async Task Handle_Throws_WhenUserNotFound()
  {
    // Arrange
    var act = () => new UpdateUserRatingHandler(_context)
      .Handle(new UpdateUserRating { UserId = Guid.NewGuid(), Rating = 5 }, CancellationToken.None);

    // Act & Assert
    var ex = await Assert.ThrowsAsync<NotFoundException>(act);
    Assert.Equal("user_not_found", ex.Message);
  }
}
