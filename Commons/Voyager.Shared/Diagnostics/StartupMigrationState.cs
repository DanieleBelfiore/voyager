namespace Voyager.Shared.Diagnostics;

/// <summary>
/// Shared between the migration hosted service and the readiness health check. The server is
/// already listening while migrations run, so /health (liveness) answers immediately — this flag
/// is what keeps /ready (readiness) reporting unhealthy until the schema is actually usable.
/// </summary>
public class StartupMigrationState
{
  public bool Completed { get; private set; }
  public bool Failed { get; private set; }

  public void MarkCompleted() => Completed = true;

  public void MarkFailed() => Failed = true;
}
