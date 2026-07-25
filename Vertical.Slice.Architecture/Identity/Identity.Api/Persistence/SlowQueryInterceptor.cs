using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Persistence;

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
