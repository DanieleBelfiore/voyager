using Microsoft.EntityFrameworkCore;
using Ride.Handlers.Models;
using Xunit;
using RideEntity = Ride.Handlers.Models.Ride;

namespace Ride.Tests.Models;

/// <summary>
/// "One active ride per user" is enforced by a unique filtered index, not by the AnyAsync check
/// in RequestRideHandler — two concurrent requests can both pass that check. The race itself
/// only reproduces against real SQL Server (SqlException isn't constructible, so the handler's
/// DbUpdateException catch can't be unit-tested; Ride.IntegrationTests covers it via
/// Testcontainers). What is checkable here is that the index is still declared at all: this
/// variant shipped without it while the other four had it.
/// </summary>
public class RideContextIndexTests
{
  [Fact]
  public void RideModel_DeclaresUniqueFilteredIndex_ForOneActiveRidePerUser()
  {
    // Arrange
    using var db = new RideContext(new DbContextOptionsBuilder<RideContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

    // Act
    var index = db.Model.FindEntityType(typeof(RideEntity))!
      .GetIndexes()
      .Single(i => i.GetDatabaseName() == "IX_Rides_UserId_ActiveOnly");

    // Assert
    Assert.True(index.IsUnique);
    Assert.Equal(nameof(RideEntity.UserId), Assert.Single(index.Properties).Name);

    // Requested / DriverAssigned / InProgress — a completed or cancelled ride must not block a
    // new request, so the uniqueness only applies over the in-flight statuses.
    Assert.Equal("[Status] IN (0, 1, 2)", index.GetFilter());
  }
}
