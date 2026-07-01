using System.Text;

namespace Cmoll.Compiler.Terms;

record Term
{
  public InputStatus Status = InputStatus.Empty;

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

  /// <summary>
  /// Adapts the priority to the args
  /// (braces did set it to 1, so we will correct it here)
  /// </summary>
  public virtual void FixPrio() { }
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
  public override string ToString() => base.ToString();
}

record StringTerm(string QuotedValue) : Term
{

  public string UnquotedValue
  {
    get
    {
      return QuotedValue.Replace("\"\"", "\"")[1..^1];
    }
  }

  public override void BuildCoreString(StringBuilder sb) => sb.Append(QuotedValue);
  public override string ToString() => base.ToString();
}
record Name(string Text) : Term
{
  public override void BuildCoreString(StringBuilder sb) => sb.Append(Text);
  public override string ToString() => base.ToString();
}

record ParenTerm(Term? Inner) : Term
{
  public override void BuildCoreString(StringBuilder sb)
  {
    sb.Append('(');
    Inner?.BuildCoreString(sb);
    sb.Append(')');
  }
  public override string ToString() => base.ToString();

  public Term[] ConvertToArray()
  {
    List<Term> res = [];
    var t = Inner;
    while (t != null)
    {
      if (t is OpTerm ot && ot.op.Symbol == ",")
      {
        res.Add(ot.arg1!);
        t = ot.arg0;
      }
      res.Add(t); break;
    }
    res.Reverse();
    return [.. res];
  }
}

record CallTerm(Term Func, Term[] Args) : Term
{
  public override void BuildCoreString(StringBuilder sb)
  {
    sb.Append('(');
    foreach (Term t in Args) t.BuildCoreString(sb);
    sb.Append(')');
  }
  public override string ToString() => base.ToString();
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
      if (arg.Prio > cmpPrio) sb.Append('(');
      arg.BuildCoreString(sb);
      if (arg.Prio > cmpPrio) sb.Append(')');
    }
  }
  public override void FixPrio()
  {
    arg0.FixPrio(); arg1?.FixPrio();
    //if (arg0.Prio > this.Prio || arg1?.Prio > this.Prio) throw new NotSupportedException();
    this.Prio = op.Priority;
  }

  public override string ToString() => base.ToString();
}