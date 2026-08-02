using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Driver.Handlers.Interfaces;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Toolbelt.ComponentModel.DataAnnotations;

namespace Driver.Handlers.Models;

public class SQLMigrationContext(DbContextOptions<DriverContext> options) : DriverContext(options)
{
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    base.OnConfiguring(optionsBuilder);
    optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
  }
}

public class DriverContext(DbContextOptions<DriverContext> options) : DbContext(options), IDriverContext
{
  public DbSet<Driver> Drivers { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.BuildIndexesFromAnnotations();
  }

  public new void Add<TEntity>(TEntity entity) where TEntity : class
  {
    Set<TEntity>().Add(entity);
  }
}

[UsedImplicitly]
public class DriverSQLContextFactory : IDesignTimeDbContextFactory<SQLMigrationContext>
{
  public SQLMigrationContext CreateDbContext(string[] args)
  {
    var optionsBuilder = new DbContextOptionsBuilder<DriverContext>();
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
