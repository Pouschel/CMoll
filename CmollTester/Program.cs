global using Cmoll.Compiler.Core;
global using Cmoll.Compiler.Scanning;
global using Cmoll.Compiler.Parsing;
global using Cmoll.Compiler;
global using Testing.Framework;
global using static Testing.Framework.Assert;

namespace CmollTester;

internal class Program
{
  static void Main(string[] args)
  {
    var term= TermTester.CompileOpTerm("1+8");
    var s = term?.ToString();
    PredCompExperiment.PrintSpf(12);

    Tester.Start();
    TermTester.TestAllTerms();
    //Tester.TestAssembly(typeof(Program).Assembly);
    Tester.End();


  }
}
