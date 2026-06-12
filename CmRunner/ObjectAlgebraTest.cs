using System;
using System.Collections.Generic;
using System.Text;
using CmRunner;

namespace CmRunner;

class Exp
{
}
class Lit : Exp
{
  private int x;

  public Lit(int x)
  {
    this.x = x;
  }
}
class Add : Exp
{
  public Add(Exp e1, Exp e2)
  {
    this.Lhs = e1;
    this.Rhs = e2;
  }

  public Exp Lhs { get; }
  public Exp Rhs { get; }
}

interface IntAlg<A>
{
  A lit(int x);
  A add(A e1, A e2);
}


class IntFactory : IntAlg<Exp>
{
  public Exp lit(int x)
  {
    return new Lit(x);
  }
  public Exp add(Exp e1, Exp e2)
  {
    return new Add(e1, e2);
  }
}

interface IEvalExpr
{
  int Eval();
}

class EvalAlgebra : IntAlg<IEvalExpr>
{
  public IEvalExpr add(IEvalExpr e1, IEvalExpr e2) => new Add(e1, e2);
  public IEvalExpr lit(int x) => new Lit(x);

  record Lit(int v) : IEvalExpr
  {
    public int Eval() => v;
  }
  record Add(IEvalExpr a, IEvalExpr b) : IEvalExpr
  {
    public int Eval() => a.Eval() + b.Eval();
  }
}

interface IPrint
{
  string print();
}


class IntPrint : IntAlg<IPrint>
{
  public IPrint add(IPrint e1, IPrint e2) => new Add(e1, e2);
  public IPrint lit(int x) => new Lit(x);

  record Lit(int x) : IPrint
  {
    public string print() => x.ToString();
  }

  record Add(IPrint a, IPrint b) : IPrint
  {
    public string print() => a.print() + "+" + b.print();
  }
}


public class TMain
{
  static A make3Plus5<A>(IntAlg<A> f) => f.add(f.lit(3), f.lit(5));

  public static void test()
  {
    EvalAlgebra evAlg = new();
    IntPrint print = new();
    int x = make3Plus5(evAlg).Eval(); // int x = exp.eval();
    String s = make3Plus5(print).print();
    Console.WriteLine(s);

  }
}