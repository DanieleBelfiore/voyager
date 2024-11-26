using Identity.Handlers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ride.Handlers.Interfaces;

namespace Ride.Tests
{
  public class TestApplicationDbContext :
      IdentityDbContext<VoyagerUser, VoyagerRole, Guid, IdentityUserClaim<Guid>, IdentityUserRole<Guid>,
      IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>,
      IRideContext
  {
    public TestApplicationDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Ride.Handlers.Models.Ride> Rides { get; set; }

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
