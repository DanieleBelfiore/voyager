using Driver.Module.Features.AddDriver;
using Driver.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DriverEntity = Driver.Module.Entities.Driver;

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

    Assert.NotNull((await db.Drivers.FindAsync(id)));
  }

  [Fact]
  public async Task AddDriver_ShouldBeIdempotent_WhenDriverAlreadyExists()
  {
    await using var db = NewContext();
    var id = Guid.NewGuid();
    db.Drivers.Add(new DriverEntity(id));
    await db.SaveChangesAsync();

    var handler = new AddDriverHandler(db);
    await handler.Handle(new Voyager.Contracts.Driver.AddDriver { DriverId = id }, CancellationToken.None);

    Assert.Equal(1, db.Drivers.Count(d => d.Id == id));
  }
}
