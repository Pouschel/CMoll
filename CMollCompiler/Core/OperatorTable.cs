using System.Diagnostics;
using System.Xml.Linq;

namespace Cmoll.Compiler.Core;

record OperatorInfo(string Text, string Spec, int Priority)
{
  public bool IsUnary => Arity == 1;
  public bool IsBinary => Arity == 2;
  public int Arity => Spec.Length - 1;
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
}
