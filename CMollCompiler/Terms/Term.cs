using System.Text;

namespace Cmoll.Compiler.Terms;

abstract class Term
{
  public InputStatus Status = InputStatus.Empty;

  public BaseType Type = BaseType.NotResolved;
  public int Prio = 1;

  public abstract R Accept<R>(TermVisitor<R> visitor);

  public virtual void BuildCoreString(StringBuilder sb)
  {
  }
  public override string ToString()
  {
    return $"{CoreString} {Type} {Prio}";
  }

  public string CoreString
  {
    get
    {
      var sb = new StringBuilder();
      BuildCoreString(sb);
      return sb.ToString();
    }
  }
  /// <summary>
  /// Adapts the priority to the args
  /// (braces did set it to 1, so we will correct it here)
  /// </summary>
  
}

class NumberTerm(string value) : Term
{
  public NumberTerm(string value, CsType type) : this(value)
  {
    this.Type = type;
  }
  public string Value => value;

  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitNumberTerm(this);
  public override void BuildCoreString(StringBuilder sb) => sb.Append(value);
 
}

class StringTerm(string quotedValue) : Term
{
  public string UnquotedValue
  {
    get
    {
      return quotedValue.Replace("\"\"", "\"")[1..^1];
    }
  }

  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitStringTerm(this);
  public override void BuildCoreString(StringBuilder sb) => sb.Append(quotedValue);
  public override string ToString() => base.ToString();
}
class NameTerm(string text) : Term
{
  public string Text => text;

  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitNameTerm(this);
  public override void BuildCoreString(StringBuilder sb) => sb.Append(text);
  public override string ToString() => base.ToString();
}

class ParenTerm(Term? inner) : Term
{
  public Term Inner => inner!;
  public override void BuildCoreString(StringBuilder sb)
  {
    sb.Append('(');
    inner?.BuildCoreString(sb);
    sb.Append(')');
  }
  public override string ToString() => base.ToString();

  public Term[] ConvertToArray()
  {
    List<Term> res = [];
    var t = inner;
    while (t != null)
    {
      if (t is OpTerm ot && ot.Op.Symbol == ",")
      {
        res.Add(ot.Arg1!);
        t = ot.Arg0;
      }
      res.Add(t); break;
    }
    res.Reverse();
    return [.. res];
  }

  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitParenTerm(this);
}

class CallTerm(NameTerm func, Term[] args) : Term
{
  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitCallTerm(this);

  public override void BuildCoreString(StringBuilder sb)
  {
    sb.Append(func.Text);
    sb.Append('(');
    foreach (Term t in args) t.BuildCoreString(sb);
    sb.Append(')');
  }
}

class OpTerm(OperatorInfo op, Term arg0, Term? arg1 = null) : Term
{
  public OperatorInfo Op => op;
  public Term Arg0 => arg0;
  public Term? Arg1 => arg1;

  public override R Accept<R>(TermVisitor<R> visitor) => visitor.VisitOpTerm(this);

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
  public override string ToString() => base.ToString();
}