using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Identity.Handlers.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Toolbelt.ComponentModel.DataAnnotations;

namespace Identity.Handlers.Models
{
  public class SQLMigrationContext(DbContextOptions<IdentityContext> options) : IdentityContext(options)
  {
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      base.OnConfiguring(optionsBuilder);

      optionsBuilder.UseSqlServer();
    }
  }

  public class IdentityContext(DbContextOptions<IdentityContext> options) : IdentityDbContext<VoyagerUser, VoyagerRole, Guid>(options), IIdentityContext
  {
    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
      builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
      builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
      builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
      builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

      builder.Entity<VoyagerRole>().ToTable("Roles");

      builder.Entity<VoyagerUser>().ToTable("Users");
      builder.Entity<VoyagerUser>(entity =>
      {
        entity.Property(e => e.ConcurrencyStamp).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
        entity.Property(e => e.NormalizedEmail).HasMaxLength(256).IsRequired();
        entity.Property(e => e.NormalizedUserName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
        entity.Property(e => e.PhoneNumber).HasMaxLength(64);
        entity.Property(e => e.SecurityStamp).HasMaxLength(256).IsRequired();
        entity.Property(e => e.UserName).HasMaxLength(256).IsRequired();
        entity.Property(e => e.Created).HasColumnType("DateTime");
        entity.Property(e => e.Modified).HasColumnType("DateTime");
      });

      builder.BuildIndexesFromAnnotations();
    }

    public new void Add<TEntity>(TEntity entity) where TEntity : class
    {
      Set<TEntity>().Add(entity);
    }
  }

  public class IdentitySQLContextFactory : IDesignTimeDbContextFactory<SQLMigrationContext>
  {
    public SQLMigrationContext CreateDbContext(string[] args)
    {
      var optionsBuilder = new DbContextOptionsBuilder<IdentityContext>();
      optionsBuilder.UseSqlServer();
      optionsBuilder.UseOpenIddict();
      return new SQLMigrationContext(optionsBuilder.Options);
    }
  }

  public class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
  {
    private const int SlowQueryThreshold = 1;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
      if (eventData.Duration.Seconds >= SlowQueryThreshold)
        logger.LogWarning($"Slow query detected ({eventData.Duration.TotalMilliseconds} ms): {command.CommandText}");

      return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
  }
}
