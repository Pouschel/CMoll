


using CMoll.Compiler;

class Program
{
  static void Main(string[] args)
  {
    var opt = new CmcOptions()
    {
      OutputDir=@"R:\CmcTest",
      SourceFile="a.cmoll"
    };
    CmcMain.CompileAndRun(opt);

  }
}

