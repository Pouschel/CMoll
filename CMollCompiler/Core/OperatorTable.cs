using System.Diagnostics;
using System.Xml.Linq;

namespace Cmoll.Compiler.Core;

record OperatorInfo(string Text, string Spec, int Priority)
{
  public bool IsUnary => Arity == 1;
  public bool IsBinary => Arity == 2;
  public int Arity => Spec.Length - 1;

  public bool IsInfix => Arity == 2 && Spec[1] == 'f';
  public bool IsPrefix => Arity == 1 && Spec[0] == 'f';
  public bool IsPostfix => Arity == 1 && Spec[1] == 'f';

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
  internal OperatorInfo? GetRhsOperator(string opName)
  {
    // situation lhs op ?
    // this function returs therfore the op iff the f is on pos 1, e.g. xf. yf. 
    if (!data.TryGetValue(opName, out var ov)) return null;
    while (ov != null)
    {
      if (ov.Op.Spec[1] == 'f') return ov.Op;
      ov = ov.Next;
    }
    return null;
  }
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
      if (op?.Priority<minPrio)
      {
        minPrio= op.Priority;
        bestIndex = i;
        bestOp = op;
      }

      OperatorInfo? CheckAllowed()
      {
        OperatorInfo? resop = null;
        int minPrio=int.MaxValue;
        while (ov != null)
        {
          var op = ov.Op;
          if (op.IsInfix && (i == 0 || i == list.Count - 1)) continue;
          if (op.IsPrefix && i == list.Count - 1) continue;
          if (op.IsPostfix && i == 0) continue;
          if (op.Priority<minPrio)
          {
            minPrio= op.Priority;
            resop = op;
          }
          ov = ov.Next;
        }
        return resop;
      }
    }
    return (bestIndex, bestOp);
  }
}
