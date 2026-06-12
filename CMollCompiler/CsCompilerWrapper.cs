using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

class CsCompilerWrapper
{

  public static int Compile(CsCompilerOptions options)
  {
    var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

    var syntaxTrees = options.SourceFiles
        .Select(path => CSharpSyntaxTree.ParseText(
            SourceText.From(File.ReadAllText(path), Encoding.UTF8),
            parseOptions,
            path))
        .ToArray();

    var references = BuildMetadataReferences(options.ReferenceFiles);
    var compilationOptions = new CSharpCompilationOptions(
        options.Kind,
        mainTypeName: options.MainTypeName,
        optimizationLevel: options.Release ? OptimizationLevel.Release : OptimizationLevel.Debug);

    var compilation = CSharpCompilation.Create(
        Path.GetFileNameWithoutExtension(options.AssemblyFile),
        syntaxTrees,
        references,
        compilationOptions);

    if (options.Kind == OutputKind.ConsoleApplication && compilation.GetEntryPoint(default) is null)
    {
      Console.Error.WriteLine("Error: No suitable Main method was found.");
      return 1;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.AssemblyFile))!);

    using var outputStream = File.Create(options.AssemblyFile);
    using var pdbStream = options.DebugSymbols ? File.Create(options.PdbFile) : null;
    var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb);
    var emitResult = compilation.Emit(outputStream, pdbStream: pdbStream, options: emitOptions);

    foreach (var diagnostic in emitResult.Diagnostics)
    {
      if (diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
      {
        Console.Error.WriteLine(diagnostic.ToString());
      }
    }

    if (!emitResult.Success)
    {
      Console.Error.WriteLine("Compilation failed.");
      return 1;
    }

    if (options.Kind == OutputKind.ConsoleApplication)
    {
      WriteRuntimeConfig(options.OutputFile);
      if (options.CreateAppHost)
      {
        CreateAppHost(options.OutputFile, Path.GetFileName(options.AssemblyFile));
      }
    }

    Console.WriteLine($"Created: {Path.GetFullPath(options.AssemblyFile)}");
    if (options.CreateAppHost)
    {
      Console.WriteLine($"Created: {Path.GetFullPath(options.OutputFile)}");
    }

    if (options.DebugSymbols)
    {
      Console.WriteLine($"Created: {Path.GetFullPath(options.PdbFile)}");
    }

    return 0;

  }
  static IReadOnlyList<MetadataReference> BuildMetadataReferences(IEnumerable<string> userReferences)
  {
    var references = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

    void AddReference(string? path)
    {
      if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
      {
        return;
      }

      references.TryAdd(Path.GetFullPath(path), MetadataReference.CreateFromFile(Path.GetFullPath(path)));
    }

    AddReference(typeof(object).Assembly.Location);
    AddReference(typeof(Console).Assembly.Location);
    AddReference(typeof(Enumerable).Assembly.Location);
    AddReference(typeof(List<>).Assembly.Location);
    AddReference(Assembly.Load("System.Runtime").Location);

    foreach (var path in userReferences)
    {
      if (!File.Exists(path))
      {
        throw new FileNotFoundException($"Reference assembly not found: {path}", path);
      }

      AddReference(path);
    }

    return references.Values.ToArray();
  }

  static void WriteRuntimeConfig(string outputFile)
  {
    var runtimeConfigFile = Path.ChangeExtension(outputFile, ".runtimeconfig.json");
    var runtimeVersion = Environment.Version.ToString();
    var content = $$"""
    {
      "runtimeOptions": {
        "tfm": "net10.0",
        "framework": {
          "name": "Microsoft.NETCore.App",
          "version": "{{runtimeVersion}}"
        }
      }
    }
    """;

    File.WriteAllText(runtimeConfigFile, content);
  }

  static void CreateAppHost(string appHostFile, string assemblyFileName)
  {
    var templateFile = FindAppHostTemplate();
    File.Copy(templateFile, appHostFile, overwrite: true);

    var appHostBytes = File.ReadAllBytes(appHostFile);
    var placeholderBytes = Encoding.UTF8.GetBytes("c3ab8ff13720e8ad9047dd39466b3c8974e592c2fa383d4a3960714caef0c4f2");
    var assemblyNameBytes = Encoding.UTF8.GetBytes(assemblyFileName);

    if (assemblyNameBytes.Length > placeholderBytes.Length)
    {
      throw new InvalidOperationException("The assembly file name is too long for the apphost template.");
    }

    var placeholderIndex = IndexOf(appHostBytes, placeholderBytes);
    if (placeholderIndex < 0)
    {
      throw new InvalidOperationException("The apphost template placeholder was not found.");
    }

    Array.Clear(appHostBytes, placeholderIndex, placeholderBytes.Length);
    Buffer.BlockCopy(assemblyNameBytes, 0, appHostBytes, placeholderIndex, assemblyNameBytes.Length);
    File.WriteAllBytes(appHostFile, appHostBytes);
  }

  static string FindAppHostTemplate()
  {
    var architecture = RuntimeInformation.ProcessArchitecture switch
    {
      Architecture.X64 => "x64",
      Architecture.X86 => "x86",
      Architecture.Arm64 => "arm64",
      _ => throw new PlatformNotSupportedException("Only x64, x86 and arm64 apphosts are supported.")
    };

    var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
    var dotnetRoot = Directory.GetParent(runtimeDirectory)?.Parent?.Parent?.Parent?.FullName
        ?? throw new InvalidOperationException("Unable to locate the dotnet installation directory.");
    var hostPackDirectory = Path.Combine(dotnetRoot, "packs", $"Microsoft.NETCore.App.Host.win-{architecture}");
    var versionDirectory = Directory.GetDirectories(hostPackDirectory)
        .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault();

    if (versionDirectory is null)
    {
      throw new FileNotFoundException($"No apphost pack was found in {hostPackDirectory}.");
    }

    var templateFile = Path.Combine(versionDirectory, "runtimes", $"win-{architecture}", "native", "apphost.exe");
    if (!File.Exists(templateFile))
    {
      throw new FileNotFoundException($"The apphost template was not found: {templateFile}");
    }

    return templateFile;
  }

  static int IndexOf(byte[] data, byte[] pattern)
  {
    for (var i = 0; i <= data.Length - pattern.Length; i++)
    {
      var found = true;
      for (var j = 0; j < pattern.Length; j++)
      {
        if (data[i + j] != pattern[j])
        {
          found = false;
          break;
        }
      }

      if (found)
      {
        return i;
      }
    }

    return -1;
  }


}

internal sealed class CsCompilerOptions
{
  public List<string> SourceFiles { get; } = [];
  public List<string> ReferenceFiles { get; } = [];
  public string OutputFile { get; set; } = "Output.exe";
  public string AssemblyFile => CreateAppHost ? Path.ChangeExtension(OutputFile, ".dll") : OutputFile;
  public string PdbFile => Path.ChangeExtension(AssemblyFile, ".pdb");
  public bool CreateAppHost => Kind == OutputKind.ConsoleApplication
      && string.Equals(Path.GetExtension(OutputFile), ".exe", StringComparison.OrdinalIgnoreCase);
  public OutputKind Kind { get; set; } = OutputKind.ConsoleApplication;
  public bool Release { get; set; }
  public bool DebugSymbols { get; set; }
  public bool IsValid { get; private set; }

  public string MainTypeName { get; set; } = "Program";
  public static CsCompilerOptions Parse(string[] args)
  {
    var options = new CsCompilerOptions();

    for (var i = 0; i < args.Length; i++)
    {
      var arg = args[i];
      switch (arg)
      {
        case "-o":
        case "--out":
          options.OutputFile = RequireValue(args, ref i, arg);
          break;
        case "-r":
        case "--reference":
          options.ReferenceFiles.Add(RequireValue(args, ref i, arg));
          break;
        case "--exe":
          options.Kind = OutputKind.ConsoleApplication;
          break;
        case "--dll":
          options.Kind = OutputKind.DynamicallyLinkedLibrary;
          if (options.OutputFile == "Output.exe")
          {
            options.OutputFile = "Output.dll";
          }
          break;
        case "--debug-symbols":
        case "--pdb":
          options.DebugSymbols = true;
          break;
        case "--no-debug-symbols":
          options.DebugSymbols = false;
          break;
        case "-c":
        case "--configuration":
          options.Release = string.Equals(RequireValue(args, ref i, arg), "Release", StringComparison.OrdinalIgnoreCase);
          break;
        case "-h":
        case "--help":
          options.IsValid = false;
          return options;
        default:
          options.SourceFiles.Add(arg);
          break;
      }
    }

    options.IsValid = options.SourceFiles.Count > 0
        && options.SourceFiles.All(File.Exists)
        && !string.IsNullOrWhiteSpace(options.OutputFile);

    return options;
  }

  public static void PrintUsage()
  {
    Console.WriteLine("""
        Usage:
          CSharpCompilerWrapper <source.cs> [more.cs] -r <assembly.dll> [-r <assembly.dll>] -o <target.exe|target.dll> [--exe|--dll] [--debug-symbols]

        Examples:
          CSharpCompilerWrapper Hello.cs -o Hello.exe
          CSharpCompilerWrapper Program.cs -r MyLibrary.dll -o App.exe --exe --debug-symbols
        """);
  }

  private static string RequireValue(string[] args, ref int index, string optionName)
  {
    if (index + 1 >= args.Length)
    {
      throw new ArgumentException($"Option {optionName} requires a value.");
    }

    index++;
    return args[index];
  }
}

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
  public static int RunEntryPoint(string assemblyFile, IReadOnlyList<string> applicationArguments, bool waitForUnload=false)
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
