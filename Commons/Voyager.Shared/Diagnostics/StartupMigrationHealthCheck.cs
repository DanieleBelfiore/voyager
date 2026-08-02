using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Voyager.Shared.Diagnostics;

/// <summary>
/// Readiness gate for the startup migration. A plain DbContext connectivity check isn't enough on
/// its own: the database answers while a migration is only half-applied, so /ready would go green
/// against a schema the app can't actually use yet.
/// </summary>
internal sealed class StartupMigrationHealthCheck(StartupMigrationState state) : IHealthCheck
{
  public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    if (state.Failed)
      return Task.FromResult(HealthCheckResult.Unhealthy("database migration failed"));

    return Task.FromResult(state.Completed
      ? HealthCheckResult.Healthy()
      : HealthCheckResult.Unhealthy("database migration in progress"));
  }
}
