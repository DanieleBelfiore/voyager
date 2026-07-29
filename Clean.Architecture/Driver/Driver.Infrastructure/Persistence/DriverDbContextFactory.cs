using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Driver.Infrastructure.Persistence;

public class DriverDbContextFactory : IDesignTimeDbContextFactory<DriverDbContext>
{
  public DriverDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<DriverDbContext>();
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    return new DriverDbContext(optionsBuilder.Options);
  }
}
