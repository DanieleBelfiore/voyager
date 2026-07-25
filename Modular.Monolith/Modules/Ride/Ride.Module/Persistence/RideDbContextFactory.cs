using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ride.Module.Persistence;

internal class RideDbContextFactory : IDesignTimeDbContextFactory<RideDbContext>
{
  public RideDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<RideDbContext>();
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    return new RideDbContext(optionsBuilder.Options);
  }
}
