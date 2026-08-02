using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Ride.Handlers.Interfaces;
using Toolbelt.ComponentModel.DataAnnotations;

namespace Ride.Handlers.Models;

public class SQLMigrationContext(DbContextOptions<RideContext> options) : RideContext(options)
{
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
  }
}

public class RideContext(DbContextOptions<RideContext> options) : DbContext(options), IRideContext
{
  public DbSet<Ride> Rides { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.BuildIndexesFromAnnotations();

    modelBuilder.Entity<Ride>().Property(r => r.RowVersion).IsRowVersion();

    // Enforces "one active ride per user" at the DB level, mirroring the in-memory check in
    // RequestRideHandler so a race between two concurrent requests can't both pass that check
    // and insert two in-flight rides for the same user. Fluent rather than an [Index] attribute
    // because BuildIndexesFromAnnotations cannot express a filtered index. Statuses 0/1/2 are
    // Requested/DriverAssigned/InProgress.
    modelBuilder.Entity<Ride>()
      .HasIndex(r => r.UserId)
      .HasDatabaseName("IX_Rides_UserId_ActiveOnly")
      .IsUnique()
      .HasFilter("[Status] IN (0, 1, 2)");
  }

  public new void Add<TEntity>(TEntity entity) where TEntity : class
  {
    Set<TEntity>().Add(entity);
  }
}

[UsedImplicitly]
public class RideSQLContextFactory : IDesignTimeDbContextFactory<SQLMigrationContext>
{
  public SQLMigrationContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<RideContext>();
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    return new SQLMigrationContext(optionsBuilder.Options);
  }
}

public class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
{
  private const double SlowQueryThresholdSeconds = 1;

  public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
  {
    // TotalSeconds, not Seconds: TimeSpan.Seconds is the 0-59 component, so a 60s query
    // reported 0 and was never logged while a 61s one reported 1 and was.
    if (eventData.Duration.TotalSeconds >= SlowQueryThresholdSeconds)
      logger.LogWarning("Slow query detected ({ElapsedMilliseconds} ms): {CommandText}", eventData.Duration.TotalMilliseconds, command.CommandText);

    return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
  }
}
