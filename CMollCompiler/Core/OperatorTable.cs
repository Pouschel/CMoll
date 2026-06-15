using System.Diagnostics;
using System.Xml.Linq;

namespace Cmoll.Compiler.Core;

class OperatorInfo(string text, int priority) : IEquatable<OperatorInfo>
{
  string spec;
  public OperatorInfo(string text, string spec, int priority) : this(text, priority)
  {
    this.spec = spec;
    Arity = spec.Length - 1;
    var test = spec.Replace("f", "");
    MaxPrioArg0 = test[0] == 'y' ? priority : priority - 1;
    MaxPrioArg1 = test.Length == 1 ? int.MaxValue : (test[1] == 'y' ? priority : priority - 1);
  }
  public string Symbol => text;
  public string SpecString => spec;
  public int Priority => priority;
  public bool IsUnary => Arity == 1;
  public bool IsBinary => Arity == 2;
  public int Arity { get; init; }
  public int MaxPrioArg0 { get; init; }
  public int MaxPrioArg1 { get; init; }

  public bool IsInfix => Arity == 2 && spec[1] == 'f';
  public bool IsPrefix => Arity == 1 && spec[0] == 'f';
  public bool IsPostfix => Arity == 1 && spec[1] == 'f';

  public override bool Equals(object? obj) => Equals(obj as OperatorInfo);
  public override int GetHashCode() => Symbol.GetHashCode() << 3 + Priority + spec.GetHashCode();
  public bool Equals(OperatorInfo? other)
  {
    if (other is null) return false;
    if (this.Symbol != other.Symbol) return false;
    if (this.spec != other.spec) return false;
    if (this.Priority != other.Priority) return false;
    return true;
  }

  public override string ToString() => $"{Symbol} {spec} {priority}";
}


class OperatorTable
{
  record OpValue(OperatorInfo Op)
  {
    public OpValue? Next;
  }
  Dictionary<string, OpValue> data = [];
  public OperatorTable()
  {
    Init();
  }
  void Init()
  {
    Add("*", "yfx", 400);
    Add("+", "yfx", 500);
  }
  public void Add(string text, string spec, int prio)
  {
    Debug.Assert(text.Length > 0 && spec.Length >= 2 && spec.Contains('f') && prio > 0);
    var oi = new OperatorInfo(text, spec, prio);
    if (data.TryGetValue(text, out var ov))
      ov.Next = new(oi);
    else
      data[text] = new(oi);
  }
  public OperatorInfo? GetWithArity(string name, int arity)
  {
    if (!data.TryGetValue(name, out var ov)) return null;
    while (ov != null)
    {
      if (ov.Op.Arity == arity) return ov.Op;
      ov = ov.Next;
    }
    return null;
  }
  internal bool ContainsOperator(string opName) => data.ContainsKey(opName);
  public (int, OperatorInfo?) FindBestMatchOp(List<object> list)
  {
    int minPrio = int.MaxValue;
    int bestIndex = -1;
    OperatorInfo? bestOp = null;
    for (int i = 0; i < list.Count; i++)
    {
      if (list[i] is not Token tok) continue;
      if (!data.TryGetValue(tok.StringValue, out var ov))
        throw CmcException.Create(Invalid_operator, tok.Status, tok.StringValue);
      var op = CheckAllowed();
      if (op?.Priority < minPrio)
      {
        minPrio = op.Priority;
        bestIndex = i;
        bestOp = op;
      }

      OperatorInfo? CheckAllowed()
      {
        OperatorInfo? resop = null;
        int minPrio = int.MaxValue;
        while (ov != null)
        {
          var op = ov.Op;
          try
          {
            if (op.IsInfix && (i == 0 || i == list.Count - 1)) continue;
            if (op.IsPrefix && i == list.Count - 1) continue;
            if (op.IsPostfix && i == 0) continue;
            if (op.Priority < minPrio)
            {
              minPrio = op.Priority;
              resop = op;
            }
          }
          finally
          {
            ov = ov.Next;
          }
        }
        return resop;
      }
    }
    return (bestIndex, bestOp);
  }
}
