using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ride.Module.Persistence;

internal class SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger) : DbCommandInterceptor
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
