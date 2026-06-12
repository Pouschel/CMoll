using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace CsHelper;

class CsRunner
{
  public static async Task<int> RunEntryPointAsync(string assemblyFile, IReadOnlyList<string> applicationArguments)
  {
    var (exitCode, weakReference) = await RunEntryPointInCollectibleContextAsync(
        Path.GetFullPath(assemblyFile),
        applicationArguments).ConfigureAwait(false);

    WaitForUnload(weakReference);
    return exitCode;
  }


  public static int RunEntryPoint(string assemblyFile, bool waitForUnload = false)
    => RunEntryPoint(assemblyFile, [], waitForUnload);
  public static int RunEntryPoint(string assemblyFile, IReadOnlyList<string> applicationArguments, bool waitForUnload = false)
  {
    var assemblyPath = Path.GetFullPath(assemblyFile);
    var loadContext = new CollectibleAssemblyLoadContext(assemblyPath);
    var weakReference = new WeakReference(loadContext, trackResurrection: false);
    var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
    var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("The compiled assembly has no entry point.");

    var parameters = entryPoint.GetParameters();
    var invokeArguments = parameters.Length switch
    {
      0 => null,
      1 when parameters[0].ParameterType == typeof(string[]) => new object[] { applicationArguments.ToArray() },
      _ => throw new InvalidOperationException("The Main method must have no parameters or a single string[] parameter.")
    };

    var result = entryPoint.Invoke(null, invokeArguments);
    if (result is not int exitCode)
      throw new InvalidOperationException("no int return from main");
    loadContext.Unload();
    if (waitForUnload)
      WaitForUnload(weakReference);
    return exitCode;
  }


  [MethodImpl(MethodImplOptions.NoInlining)]
  static async Task<(int ExitCode, WeakReference LoadContextReference)> RunEntryPointInCollectibleContextAsync(
      string assemblyPath, IReadOnlyList<string> applicationArguments)
  {
    var loadContext = new CollectibleAssemblyLoadContext(assemblyPath);
    var weakReference = new WeakReference(loadContext, trackResurrection: false);

    var exitCode = await InvokeEntryPointAsync(loadContext, assemblyPath, applicationArguments).ConfigureAwait(false);
    loadContext.Unload();
    // Return only a weak reference so the caller can verify unloading after this method's
    // strong local references to the load context, assembly, entry point and result are gone.

    return (exitCode, weakReference);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static async Task<(int ExitCode, WeakReference LoadContextReference)> RunEntryPointInCollectibleContext(
    string assemblyPath, IReadOnlyList<string> applicationArguments)
  {
    var loadContext = new CollectibleAssemblyLoadContext(assemblyPath);
    var weakReference = new WeakReference(loadContext, trackResurrection: false);

    var exitCode = await InvokeEntryPointAsync(loadContext, assemblyPath, applicationArguments).ConfigureAwait(false);
    loadContext.Unload();
    // Return only a weak reference so the caller can verify unloading after this method's
    // strong local references to the load context, assembly, entry point and result are gone.
    return (exitCode, weakReference);
  }

  static async Task<int> InvokeEntryPointAsync(AssemblyLoadContext loadContext,
      string assemblyPath, IReadOnlyList<string> applicationArguments)
  {
    var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
    var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("The compiled assembly has no entry point.");

    var parameters = entryPoint.GetParameters();
    var invokeArguments = parameters.Length switch
    {
      0 => null,
      1 when parameters[0].ParameterType == typeof(string[]) => new object[] { applicationArguments.ToArray() },
      _ => throw new InvalidOperationException("The Main method must have no parameters or a single string[] parameter.")
    };

    var result = entryPoint.Invoke(null, invokeArguments);
    if (result is Task task)
    {
      await task.ConfigureAwait(false);
      var resultProperty = task.GetType().GetProperty("Result");
      return resultProperty?.GetValue(task) is int taskExitCode ? taskExitCode : 0;
    }

    return result is int exitCode ? exitCode : 0;
  }

  static void WaitForUnload(WeakReference weakReference)
  {
    for (var i = 0; weakReference.IsAlive && i < 10; i++)
    {
      GC.Collect();
      GC.WaitForPendingFinalizers();
      GC.Collect();
    }

    if (weakReference.IsAlive)
    {
      Console.Error.WriteLine("Warning: The compiled assembly could not be unloaded. It may still have active references, threads, timers or event handlers.");
    }
  }

}


internal sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext
{
  private readonly AssemblyDependencyResolver _resolver;

  public CollectibleAssemblyLoadContext(string mainAssemblyPath)
      : base(isCollectible: true)
  {
    _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
  }

  protected override Assembly? Load(AssemblyName assemblyName)
  {
    var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
    return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
  }

  protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
  {
    var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
    return libraryPath is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
  }
}
