namespace Cmoll.Compiler.Parsing;

abstract record Expr
{
  public InputStatus Status = InputStatus.Empty;
  public override string ToString()
    => Status.IsEmpty ? Status.ReadPartialText() : $"a {this.GetType().Name}";
}



record ModulExpr(string Name) : Expr
{
}


