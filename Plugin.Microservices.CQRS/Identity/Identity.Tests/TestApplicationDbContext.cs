using Identity.Handlers.Interfaces;
using Identity.Handlers.Models;
using Microsoft.EntityFrameworkCore;

namespace Identity.Tests;

public class TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : DbContext(options), IIdentityContext
{
  public DbSet<VoyagerUser> Users { get; set; } = null!;

  public new void Add<TEntity>(TEntity entity) where TEntity : class
  {
    base.Add(entity);
  }

  public new async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
  {
    return await base.SaveChangesAsync(cancellationToken);
  }
}
