using System;
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

namespace Common.Core
{
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
    private Loader()
    {
    }

    public static Loader Current { get; } = new();
    public List<string> Directories { get; } = [];
    public IEnumerable<IModule> Modules { get; set; }
    public IEnumerable<Assembly> Assemblies { get; private set; }

    public void Compose()
    {
      // Catalogs does not exists in Dotnet Core, so you need to manage your own.
      var assemblies = new List<Assembly> { Assembly.GetEntryAssembly() };
      var modules = new List<IModule>();

      // All dlls in given directories except runtimes folder
      foreach (var dir in Directories)
      {
        var files = Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories).Where(f => !f.Contains("runtimes"));
        foreach (var f in files)
          try
          {
            var loadContext = new ModuleLoader(dir);

            var s = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(f)));
            if (s.GetTypes().Any(p => typeof(IModule).IsAssignableFrom(p)))
              assemblies.Add(s);
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.Message);
            Console.WriteLine(f);
          }
      }

      foreach (var m in assemblies.SelectMany(a => a.GetTypes().Where(p => typeof(IModule).IsAssignableFrom(p) && !p.IsInterface)))
        try
        {
          modules.Add(Activator.CreateInstance(m) as IModule);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
          Console.WriteLine(m);
        }

      Assemblies = assemblies;
      Modules = modules;
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
  }

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
}
