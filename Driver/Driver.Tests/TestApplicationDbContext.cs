using Driver.Handlers.Interfaces;
using Identity.Handlers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Driver.Tests
{
  public class TestApplicationDbContext(DbContextOptions options) :
    IdentityDbContext<VoyagerUser, VoyagerRole, Guid, IdentityUserClaim<Guid>, IdentityUserRole<Guid>,
    IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>(options),
    IDriverContext
  {
    public DbSet<Driver.Handlers.Models.Driver> Drivers { get; set; }

    public new void Add<TEntity>(TEntity entity) where TEntity : class
    {
      base.Add(entity);
    }

    public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
      return await base.SaveChangesAsync(cancellationToken);
    }
  }
}
