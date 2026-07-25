using Driver.Api.Features.AddDriver;
using Driver.Api.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Driver.Tests.Features;

public class AddDriverHandlerTests
{
  private static DriverDbContext NewContext()
  {
    var options = new DbContextOptionsBuilder<DriverDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    return new DriverDbContext(options);
  }

  [Fact]
  public async Task AddDriver_ShouldPersist_WhenNew()
  {
    await using var db = NewContext();
    var handler = new AddDriverHandler(db);
    var id = Guid.NewGuid();

    await handler.Handle(new Voyager.Contracts.Driver.AddDriver { DriverId = id }, CancellationToken.None);

    (await db.Drivers.FindAsync(id)).Should().NotBeNull();
  }

  [Fact]
  public async Task AddDriver_ShouldBeIdempotent_WhenDriverAlreadyExists()
  {
    await using var db = NewContext();
    var id = Guid.NewGuid();
    db.Drivers.Add(new Driver.Api.Entities.Driver(id));
    await db.SaveChangesAsync();

    var handler = new AddDriverHandler(db);
    await handler.Handle(new Voyager.Contracts.Driver.AddDriver { DriverId = id }, CancellationToken.None);

    db.Drivers.Count(d => d.Id == id).Should().Be(1);
  }
}
