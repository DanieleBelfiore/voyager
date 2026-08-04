using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Common.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.Core;

/// <summary>
/// Dynamic module loader implementing a plugin architecture.
/// Enables:
/// - Runtime discovery and loading of service modules
/// - Dependency injection configuration
/// - Automated service registration
/// - Module lifecycle management
///
/// Core component for maintaining system modularity and extensibility
/// </summary>
public class Loader
{
  private readonly List<SkippedFile> _skippedFiles = [];
  private readonly HashSet<string> _composedDirectories = new(StringComparer.Ordinal);
  private readonly HashSet<Assembly> _assemblies = [];
  private readonly object _gate = new();

  private Loader()
  {
  }

  public static Loader Current { get; } = new();

  /// <summary>
  /// Content roots to probe for modules. A <see cref="ConcurrentBag{T}"/> rather than a
  /// <see cref="List{T}"/> because hosts add to it from their own startup path while
  /// <see cref="Compose"/> may already be enumerating it — see the remarks on that method.
  /// </summary>
  public ConcurrentBag<string> Directories { get; } = [];

  public IEnumerable<IModule> Modules { get; set; }
  public IEnumerable<Assembly> Assemblies { get; private set; }

  /// <summary>
  /// Files under <see cref="Directories"/> that could not be probed as managed assemblies —
  /// native and non-.NET DLLs land here and genuinely are not modules, so this is not an error
  /// condition. It is recorded rather than discarded because "the module silently failed to
  /// load" was previously indistinguishable from "the module registered nothing", and the first
  /// case only surfaced much later as an unrelated NullReferenceException at request time.
  /// </summary>
  public IReadOnlyList<SkippedFile> SkippedFiles
  {
    get
    {
      lock (_gate)
      {
        return _skippedFiles.ToArray();
      }
    }
  }

  /// <summary>
  /// Discovers and instantiates every <see cref="IModule"/> reachable from <see cref="Directories"/>.
  /// </summary>
  /// <remarks>
  /// Called once per host boot (<c>Program.cs</c> of each service) against a process-wide
  /// singleton. That held only as long as a process ran exactly one host; integration tests stand
  /// up several <c>WebApplicationFactory</c> instances side by side, which surfaced two defects.
  /// Concurrent boots tore down the enumeration of <see cref="Directories"/> outright
  /// ("Collection was modified"), and — because every host adds the same content root — a second
  /// boot re-probed a directory already composed and registered its modules a second time,
  /// duplicating their DI registrations and EF migration runs. Hence the lock, and the
  /// deduplication by directory, by assembly, and finally by module type: an assembly resolved
  /// through two different load contexts is two distinct <see cref="Assembly"/> instances, so
  /// deduplicating the containers alone is not enough.
  /// </remarks>
  public void Compose()
  {
    lock (_gate)
    {
      // Catalogs does not exists in Dotnet Core, so you need to manage your own.
      var entryAssembly = Assembly.GetEntryAssembly();
      if (entryAssembly != null)
        _assemblies.Add(entryAssembly);

      // All dlls in given directories except runtimes folder
      foreach (var dir in Directories.Select(Path.GetFullPath).Distinct(StringComparer.Ordinal))
      {
        if (!_composedDirectories.Add(dir))
          continue;

        // One load context per directory, not per file. The previous version allocated a
        // non-collectible AssemblyLoadContext inside the file loop, so every DLL in the tree got
        // its own context: a shared dependency could be loaded many times over as mutually
        // incompatible types, and none of it was ever released.
        var loadContext = new ModuleLoader(dir);

        var files = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories).Where(f => !f.Contains("runtimes"));
        foreach (var f in files)
          try
          {
            var s = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(f)));
            if (GetLoadableTypes(s).Any(p => typeof(IModule).IsAssignableFrom(p)))
              _assemblies.Add(s);
          }
          catch (Exception ex)
          {
            _skippedFiles.Add(new SkippedFile(f, ex));
          }
      }

      var modules = new List<IModule>();
      var registered = new HashSet<string>(StringComparer.Ordinal);

      foreach (var m in _assemblies.SelectMany(GetLoadableTypes)
                 .Where(p => typeof(IModule).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract))
      {
        if (!registered.Add(m.FullName ?? m.Name))
          continue;

        // Deliberately not caught: a type that declares itself an IModule but cannot be
        // constructed is a build or packaging fault, not a probe miss. Swallowing it left the
        // service running with none of that module's DI registrations or EF migrations applied.
        modules.Add((IModule)Activator.CreateInstance(m));
      }

      Assemblies = _assemblies.ToArray();
      Modules = modules;
    }
  }

  public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment hostingEnvironment)
  {
    foreach (var m in Modules)
      m.ConfigureServices(services, configuration, hostingEnvironment);
  }

  public void AddModules(IApplicationBuilder app)
  {
    foreach (var m in Modules)
      m.OnStartup(app);
  }

  /// <summary>
  /// A partially loadable assembly still yields the types that did resolve. The caller used to
  /// invoke GetTypes() unguarded when scanning for IModule implementations, so a single
  /// unresolvable reference anywhere in the probe path aborted startup outright.
  /// </summary>
  private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
  {
    try
    {
      return assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
      return ex.Types.Where(t => t != null);
    }
  }
}

public record SkippedFile(string File, Exception Error);

public class ModuleLoader(string pluginPath) : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

  protected override Assembly Load(AssemblyName assemblyName)
  {
    var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
    return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
  }

  protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
  {
    var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
  }
}
