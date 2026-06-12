namespace CMoll.Compiler;

/// <summary>
/// Compiler options
/// </summary>
public class CmcOptions
{
  public string SourceFile = "";

  public string OutputDir = ".";

}

public class CmcResult
{
  public List<string> CsFiles = [];
}

public enum CmcErrorNumbers
{
  NoErrors = 0,
  Syntax_Error = 1000,
  Csharp_compiler_error = 9000,
}

public class CmcException : Exception
{
  public readonly CmcErrorNumbers ErrNo;

  public CmcException(CmcErrorNumbers errNo) : base(errNo.ToString().Replace('_', ' '))
  {
    this.ErrNo = errNo;
  }
}

/// <summary>
/// Main compiler 
/// </summary>
public class CmcMain
{

  static CmcResult CmollToCs(CmcOptions options)
  {
    var result = new CmcResult();
    var fakeCode = @"
using System;

public static class Program
{
  public static int Main()
  {
    Console.WriteLine($""Program executed!"");
    return 7;
  }
}

";
    if (!Directory.Exists(options.OutputDir))
      Directory.CreateDirectory(options.OutputDir);
    var fnBase = Path.GetFileNameWithoutExtension(options.SourceFile);
    var fnCs = Path.Combine(options.OutputDir, fnBase + ".cs");
    File.WriteAllText(fnCs, fakeCode);
    result.CsFiles.Add(fnCs);

    return result;
  }

  public static void CompileAndRun(CmcOptions options)
  {
    var cres = CmollToCs(options);
    var csOptions = new CsCompilerOptions
    {
      OutputFile = Path.Combine(options.OutputDir, "Main.dll"),
      DebugSymbols = true,
    };
    csOptions.SourceFiles.AddRange(cres.CsFiles);
    CsCompilerWrapper.Compile(csOptions);
    CsRunner.RunEntryPoint(csOptions.OutputFile);
  }


}

