using Identity.Api.Features.GetUsersRatings;
using Identity.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using UserEntity = Identity.Api.Entities.User;

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
    await db.SaveChangesAsync();
    var handler = new GetUsersRatingsHandler(db);

    // Act
    var result = await handler.Handle(new Voyager.Contracts.Identity.GetUsersRatings { UserIds = [user.Id] }, CancellationToken.None);

    // Assert
    Assert.Equal(user.Ratings, result[user.Id]);
  }
}
