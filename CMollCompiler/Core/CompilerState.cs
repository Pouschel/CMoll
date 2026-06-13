namespace Cmoll.Compiler.Core;

internal class CompilerState
{
  public OperatorTable OpTable = new();

  public CompilerState()
  {
    //OpTable.Add(";", "xfy", 1000);
  }
}
