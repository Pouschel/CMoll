using System.Text;

namespace Cmoll.Compiler.Parsing;

record Term : Expr
{
  public BaseType Type = BaseType.NotResolved;
  public int Prio = 1;

  public virtual void BuildCoreString(StringBuilder sb)
  {
  }
  public override string ToString()
  {
    var sb = new StringBuilder();
    BuildCoreString(sb);
    return $"{sb} {Type} {Prio}";
  }
}

record Literal : Term
{
}

record Number(string Value) : Term
{
  public Number(string value, CsType type) : this(value)
  {
    this.Type = type;
  }

  public override void BuildCoreString(StringBuilder sb) => sb.Append(Value);
}

record OpTerm(OperatorInfo op, Term arg0, Term? arg1 = null) : Term
{
  public override void BuildCoreString(StringBuilder sb)
  {
    int argsNr = 0;
    foreach (var c in op.SpecString)
    {
      if (c == 'f')
      {
        if (!op.IsPrefix) sb.Append(' ');
        sb.Append(op.Symbol);
        if (!op.IsPostfix) sb.Append(' ');
        continue;
      }
      BuildArgString(c, argsNr == 0 ? arg0 : arg1);
      argsNr++;
    }

    void BuildArgString(char xy, Term? arg)
    {
      if (arg == null) return;
      int cmpPrio = xy == 'x' ? Prio + 1 : Prio;
      if (arg.Prio > cmpPrio) sb.Append(' ');
      arg.BuildCoreString(sb);
      if (arg.Prio > cmpPrio) sb.Append(' ');
    }
  }

  public override string ToString() => base.ToString();
}