using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common.Core.Diagnostics;

/// <summary>
/// Runs EF migrations once at startup, after Kestrel is already accepting connections.
///
/// The work deliberately sits in <see cref="StartedAsync"/>, not <see cref="StartAsync"/>:
/// WebApplicationBuilder registers GenericWebHostService (the one that actually binds the port)
/// during Build(), which is *after* every AddHostedService call the application made, so a plain
/// IHostedService still blocks the socket from opening — verified by log ordering, the migration
/// completed before "Now listening" until this moved. StartedAsync runs only once every
/// StartAsync has returned, so the server is listening by then and /health (a dependency-free
/// liveness signal) answers for the whole duration of the migration.
/// </summary>
internal sealed class StartupMigrationHostedService(
  IServiceProvider services,
  Action<IServiceProvider> migrate,
  StartupMigrationState state,
  ILogger<StartupMigrationHostedService> logger) : IHostedLifecycleService
{
  public Task StartedAsync(CancellationToken cancellationToken)
  {
    try
    {
      migrate(services);
      state.MarkCompleted();

      logger.LogInformation("Startup database migration completed");
    }
    catch (Exception ex)
    {
      // Deliberately not rethrown: tearing the host down here would kill the very endpoint an
      // orchestrator polls to find out what went wrong. Staying up and failing readiness keeps
      // the container visibly unhealthy (and restartable) instead of silently gone.
      state.MarkFailed();

      logger.LogError(ex, "Startup database migration failed");
    }

    return Task.CompletedTask;
  }

  public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
