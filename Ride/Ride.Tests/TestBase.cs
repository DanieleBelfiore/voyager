using Microsoft.EntityFrameworkCore;

namespace Ride.Tests
{
  public static class TestBase
  {
    public static TestApplicationDbContext CreateTestDbContext()
    {
      return new TestApplicationDbContext(new DbContextOptionsBuilder<TestApplicationDbContext>()
                                              .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                                              .Options);
    }
  }
}
