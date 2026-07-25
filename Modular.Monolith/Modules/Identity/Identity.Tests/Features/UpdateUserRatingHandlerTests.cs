using FluentAssertions;
using Identity.Module.Features.UpdateUserRating;
using Identity.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Module.Entities.User;

namespace Identity.Tests.Features;

public class UpdateUserRatingHandlerTests
{
  [Fact]
  public async Task UpdateUserRating_ShouldRecomputeAndPersistRating()
  {
    var options = new DbContextOptionsBuilder<IdentityDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var db = new IdentityDbContext(options);
    var user = new UserEntity(Guid.NewGuid(), "ada@example.com", "Ada", "Lovelace", null, "hash", true);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    var handler = new UpdateUserRatingHandler(db);

    var result = await handler.Handle(new Voyager.Contracts.Identity.UpdateUserRating { UserId = user.Id, Rating = 5, Rides = 1 }, CancellationToken.None);

    result.Should().Be(5);
    user.Ratings.Should().Be(5);
  }
}
