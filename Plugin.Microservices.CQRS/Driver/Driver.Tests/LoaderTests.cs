using System.Collections.Concurrent;
using Common.Core;
using Xunit;
using DriverModule = Driver.Handlers.Module;

namespace Driver.Tests;

/// <summary>
/// <see cref="Loader.Current"/> is a process-wide singleton, yet every host boot does
/// <c>Directories.Add(cwd)</c> followed by <c>Compose()</c> (see each service's Program.cs).
/// A process only ever ran one host, so that was invisible — until the integration tests stood
/// up two <c>WebApplicationFactory</c> instances side by side and <c>Compose()</c> died with
/// "Collection was modified; enumeration operation may not execute" while enumerating
/// <c>Directories</c>. Composing the same directory a second time also registered every module
/// it contains twice over, duplicating each module's DI registrations and EF migrations.
/// </summary>
public class LoaderTests : IDisposable
{
  private const int ConcurrentHostBoots = 16;

  private readonly List<string> _tempDirs = [];

  public LoaderTests()
  {
    Loader.Current.Directories.Clear();
  }

  public void Dispose()
  {
    Loader.Current.Directories.Clear();

    foreach (var dir in _tempDirs)
      try
      {
        Directory.Delete(dir, true);
      }
      catch (IOException)
      {
        // A load context may still hold the copied DLL open; a leftover temp directory is not
        // worth failing a test over.
      }
  }

  /// <summary>
  /// A directory holding exactly one module assembly, standing in for a service's content root.
  /// </summary>
  private string NewModuleDirectory()
  {
    var dir = Path.Combine(Path.GetTempPath(), $"voyager-loader-{Guid.NewGuid():N}");
    Directory.CreateDirectory(dir);
    _tempDirs.Add(dir);

    var source = typeof(DriverModule).Assembly.Location;
    File.Copy(source, Path.Combine(dir, Path.GetFileName(source)));

    return dir;
  }

  [Fact]
  public async Task Compose_DoesNotThrow_WhenSeveralHostsBootConcurrentlyInTheSameProcess()
  {
    var directories = Enumerable.Range(0, ConcurrentHostBoots).Select(_ => NewModuleDirectory()).ToArray();
    var failures = new ConcurrentBag<Exception>();

    // Exactly what N hosts do at startup, minus the host itself.
    await Parallel.ForEachAsync(directories, (dir, _) =>
    {
      try
      {
        Loader.Current.Directories.Add(dir);
        Loader.Current.Compose();
      }
      catch (Exception ex)
      {
        failures.Add(ex);
      }

      return ValueTask.CompletedTask;
    });

    Assert.Empty(failures);
  }

  [Fact]
  public void Compose_RegistersEachModuleOnce_WhenTheSameDirectoryIsComposedRepeatedly()
  {
    var dir = NewModuleDirectory();

    Loader.Current.Directories.Add(dir);
    Loader.Current.Compose();

    Loader.Current.Directories.Add(dir);
    Loader.Current.Compose();

    Assert.Single(Loader.Current.Modules, m => m is DriverModule);
  }

  [Fact]
  public void Compose_RegistersEachModuleOnce_WhenTheSameAssemblyIsFoundInTwoDirectories()
  {
    // Two hosts, two content roots, both shipping the same module assembly — deduplicating by
    // directory alone is not enough.
    Loader.Current.Directories.Add(NewModuleDirectory());
    Loader.Current.Compose();

    Loader.Current.Directories.Add(NewModuleDirectory());
    Loader.Current.Compose();

    Assert.Single(Loader.Current.Modules, m => m is DriverModule);
  }
}
