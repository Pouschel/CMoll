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

class FuncExpr<V>
{
  Func<V> func;

  public FuncExpr(Func<V> func)
  {
    this.func = func;
  }

  public V Call() => func();

}

class EvalExpr : IEvalExpr
{
  public EvalExpr(Func<int> eval)
  {
    _eval = eval;
  }
  Func<int> _eval;

  public int Eval() => _eval();
}

class EvalAlgebra : IntAlg<IEvalExpr>
{
  public IEvalExpr lit(int n) => new EvalExpr(() => n);

  public IEvalExpr add(IEvalExpr a, IEvalExpr b) => new EvalExpr(() => a.Eval() + b.Eval());
}

class IntPrint : IntAlg<FuncExpr<string>>
{
  public FuncExpr<string> add(FuncExpr<string> e1, FuncExpr<string> e2) 
    => new(() => e1.Call() + "+" + e2.Call());
  public FuncExpr<string> lit(int x) => new(() => x.ToString());
}



public class TMain
{
  static A make3Plus5<A>(IntAlg<A> f) => f.add(f.lit(3), f.lit(5));

  public static void test()
  {
    EvalAlgebra evAlg = new();
    IntPrint print = new();
    int x = make3Plus5(evAlg).Eval(); // int x = exp.eval();
    String s = make3Plus5(print).Call();
    Console.WriteLine(s);

  }
}