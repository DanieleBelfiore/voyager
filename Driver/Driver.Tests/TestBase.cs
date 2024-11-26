using Common.Core.Cache;
using Microsoft.EntityFrameworkCore;

namespace Driver.Tests
{
  public static class TestBase
  {
    public static (TestApplicationDbContext context, ICacheService cache) CreateTestServices()
    {
      var context = new TestApplicationDbContext(
          new DbContextOptionsBuilder<TestApplicationDbContext>()
              .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
              .Options);

      var cache = new MemoryCacheService();

      return (context, cache);
    }
  }
}
