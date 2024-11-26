using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Ride.Handlers.Interfaces;
using Toolbelt.ComponentModel.DataAnnotations;

namespace Ride.Handlers.Models
{
  public class SQLMigrationContext(DbContextOptions<RideContext> options) : RideContext(options)
  {
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      base.OnConfiguring(optionsBuilder);
      optionsBuilder.UseSqlServer(a => a.UseNetTopologySuite());
    }
  }

  public class RideContext(DbContextOptions<RideContext> options) : DbContext(options), IRideContext
  {
    public DbSet<Ride> Rides { get; set; }

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
    private const int SlowQueryThreshold = 1;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
      if (eventData.Duration.Seconds >= SlowQueryThreshold)
        logger.LogWarning($"Slow query detected ({eventData.Duration.TotalMilliseconds} ms): {command.CommandText}");

      return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
  }
}
