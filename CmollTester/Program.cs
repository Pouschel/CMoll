global using Cmoll.Compiler;
global using Cmoll.Compiler.Core;
global using Cmoll.Compiler.Parsing;
global using Cmoll.Compiler.Scanning;
global using Testing.Framework;
global using static Testing.Framework.Assert;
using Microsoft.CodeAnalysis;

namespace CmollTester;

internal class Program
{
  static void Main(string[] args)
  {
    //var term = TermTester.CompileOpTerm("(1+8)*3");
    //var s = term?.ToString();
    //PredCompExperiment.PrintSpf(12);

    var fn = @"C:\Code\CMoll\CmCTests\MinNonEmpty.cmoll";
    TestParserOneFile(fn);

    Tester.Start();
    TermTester.TestAllTerms();
    //Tester.TestAssembly(typeof(Program).Assembly);
    Tester.End();


  }

  static void TestParserOneFile(string fn,  bool compileCs=false)
  {
    CmcOptions options = new()
    {
      SourceFile = fn,
    };
    CmcMain.ParseCmoll(options, TextWriter.Null);

  }
}
