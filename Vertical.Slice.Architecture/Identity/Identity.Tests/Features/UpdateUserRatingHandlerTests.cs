using Identity.Api.Features.UpdateUserRating;
using Identity.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Api.Entities.User;

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

    Assert.Equal(5, result);
    Assert.Equal(5, user.Ratings);
  }
}
