using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OpenIddict.EntityFrameworkCore;

namespace Identity.Api.Persistence;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
  public IdentityDbContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
    optionsBuilder.UseSqlServer();
    optionsBuilder.UseOpenIddict();
    return new IdentityDbContext(optionsBuilder.Options);
  }
}
