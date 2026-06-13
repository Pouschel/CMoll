using System;
using System.Collections.Generic;
using System.Text;

namespace CmRunner.ExtAlg;

record Exp
{ }

record Lit(int Value) : Exp
{ }

record Add(Exp Lhs, Exp Rhs) : Exp
{ }

record Bool(bool Value) : Exp
{ }

record If(Exp Cond, Exp Then, Exp Else) : Exp
{ }

public static class IntEval
{


  extension(Exp exp)
  {
    internal int Eval()
    {
      return exp switch
      {
        Lit lit => lit.Value,
        Add add => add.Lhs.Eval() + add.Rhs.Eval(),
        _ => throw new Exception()
      };
    }
    internal string Print()
    {
      return exp switch
      {
        Lit lit => lit.Value.ToString(),
        Add add => add.Lhs.Print() + " + " + add.Rhs.Print(),
        _ => throw new Exception()
      };
    }

    internal string PrintBool()
    {
      return exp switch
      {
        Bool b => b.Value.ToString(),
        If iff => $"if {iff.Cond.PrintBool()} then {iff.Then.PrintBool()} else {iff.Else.PrintBool()}",
        _ => exp.Print()
      };
    }
  }
}




internal class ExtAlgebra
{

  static Exp make3Plus5() => new Add(new Lit(3), new Lit(5));
  public static void Test()
  {
    var e = make3Plus5();
    var iv = e.Eval();
    Console.WriteLine(e.Print());
    Console.WriteLine(e.PrintBool());

  }
}
