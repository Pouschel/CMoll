using Cmoll.Compiler.Core;
using Cmoll.Compiler.Parsing;
using Cmoll.Compiler.Scanning;
using CsHelper;

namespace Cmoll.Compiler;

/// <summary>
/// Compiler options
/// </summary>
public class CmcOptions
{
  public string SourceFile = "";

  public string OutputDir = ".";

  public TextWriter ErrorWriter = Console.Out;
  public Func<InputStatus, string> InputStatusFormatter = inp => inp.Message;
  public void WriteCompilerError(in InputStatus status, string message)
  {
    var msg = string.IsNullOrEmpty(status.FileName) ? message : $"{InputStatusFormatter(status)}: Cerr: {message}";
    ErrorWriter.WriteLine(msg);
    System.Diagnostics.Trace.WriteLine(msg);
  }
}

public class CmcResult
{
  public List<string> CsFiles = [];


}

public enum CmcErrorNumbers
{
  NoErrors = 0,
  Syntax_Error = 1000,
  Invalid_token,
  Unexpected_consume, 
  Unexpected_term_token,
  Term_expected,
  Malformed_term,
  Invalid_operator,
  Csharp_compiler_error = 9000,
}



public class CmcException(CmcErrorNumbers errNo, string msg, InputStatus stat) : Exception
{

  public override string Message => base.Message;
  
  public static CmcException Create(CmcErrorNumbers errNo, InputStatus stat, params object[] args)
  {
    var msgText = errNo switch
    {
      Invalid_token => $"Invalid token '{args[0]}' found",
      Unexpected_consume => $"Expected '{args[0]}' but found '{args[1]}'",
      Unexpected_term_token  => $"Unexpected token in term: {args[0]}",
      Invalid_operator =>$"Invalid operator symbol: '{args[0]}'",
      _ => errNo.ToString().Replace('_', ' ')
    };
    return new(errNo,msgText, stat);
  }
}

/// <summary>
/// Main compiler 
/// </summary>
public class CmcMain
{

  static CmcResult CmollToCs(CmcOptions options)
  {
    var source = File.ReadAllText(options.SourceFile);
    var cstate = new CompilerState();
    var scanner = new Scanner(cstate, source, options.SourceFile);
    var tokens = scanner.ScanAllTokens();
    var parser = new Parser(cstate, options, tokens);

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

