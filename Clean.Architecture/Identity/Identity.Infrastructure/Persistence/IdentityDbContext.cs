using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UserEntity = Identity.Domain.Entities.User;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Holds our own Users table plus OpenIddict's client/token bookkeeping tables
/// (via UseOpenIddict(), configured where this context is registered). Deliberately not
/// ASP.NET Core Identity's IdentityDbContext&lt;TUser,...&gt; — we don't use UserManager/
/// RoleManager, so we don't need the dozen extra Identity tables that base class brings.
/// </summary>
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
  public DbSet<UserEntity> Users { get; set; }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);
    optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }
}
