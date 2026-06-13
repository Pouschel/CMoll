namespace Cmoll.Compiler.Parsing;

record Term : Expr
{
  public BaseType Type = BaseType.NotResolved;
  public int Prio = 1;
}

record Literal : Term
{ }

record Number(string Value) : Term
{
  public Number(string value, CsType type) : this(value)
  {
    this.Type = type;
  }
}

record OpTerm(OperatorInfo op, Term arg0, Term? arg1 = null): Term
{

}