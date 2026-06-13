using Cmoll.Compiler;

class Program
{
  static void Main(string[] args)
  {
    SimpleTests.ScannerTest(); return;
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
  public static void ScannerTest()
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, "3+4");
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);
    parser.Parse();
  }

}
