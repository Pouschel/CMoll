using Cmoll.Compiler;

class Program
{
  static void Main(string[] args)
  {
    SimpleTests.ParseTerm("3+4"); return;
    var opt = new CmcOptions()
    {
      OutputDir = @"R:\CmcTest",
      SourceFile = "a.cmoll"
    };
    CmcMain.CompileAndRun(opt);

  }
}

class SimpleTests
{

  public static void ParseTerm(string termCode, string endTermSymbol = ";")
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, termCode + endTermSymbol);
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);
    var term = parser.SubTermList(endTermSymbol);

  }

  public static void ScannerTest()
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, "3+4");
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);

    parser.Parse();
  }

}
