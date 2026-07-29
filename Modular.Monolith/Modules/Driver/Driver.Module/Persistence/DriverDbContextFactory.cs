using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Driver.Module.Persistence;

internal class DriverDbContextFactory : IDesignTimeDbContextFactory<DriverDbContext>
{
  public DriverDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<DriverDbContext>();
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    return new DriverDbContext(optionsBuilder.Options);
  }
}
