using System;
using System.Collections.Generic;
using System.Text;

namespace CmollTester;

class Var<T>
{
  public T? Value { get; set; } = default(T?);

  public Var()
  {
  }
  public Var(T? value)
  {
    Value = value;
  }
  //public static implicit operator Var<T>(T value) => new(value);

  public static implicit operator T(Var<T> varval)
  {
    if (varval.Value is null) throw new ArgumentException("variable has no value");
    return varval.Value;
  }

  public override string ToString() => Value == null ? "<unbound>" : Value.ToString();
}


internal class PredCompExperiment
{

  //:- pred square(int::in, int::out) is det. = 1 sol
  //square(X, X* X).
  public static bool Square(int x, out int y)
  {
    y = x * x;
    return true;
  }

  public static bool Sqrt(double x, Var<double> y)
  {
    y.Value = Math.Sqrt(x);
    return true;
  }

  public static bool GE_iio(double x, double y)
  {
    return x >= y;
  }
  //:- pred absolute_square_root(float::in, float::out) is semidet  : <= 1 sol
  //absolute_square_root(X, AbsSqrtX) :-
  //X >= 0.0,
  //AbsSqrtX = math.sqrt(X).
  public static bool AbsSqRoot(double x, Var<double> y)
  {
    if (!GE_iio(x, y)) return false;
    return Sqrt(x, y);
  }

  //:- pred small_prime(int::out) is multi. : >= 1 sol
  public static IEnumerable<bool> SmallPrime_o(Var<int> p)
  {
    p.Value = 2; yield return true;
    p.Value = 3; yield return true;
    p.Value = 5; yield return true;
    p.Value = 7; yield return true;
  }

  public static void PrintPrimes()
  {
    Var<int> x = new();
    foreach (var p in SmallPrime_o(x))
    {
      Console.WriteLine(x.ToString());
    }
  }

  public static bool Eq(int x, int y)
  {
    return x == y;
  }

  public static bool Eq<T>(T x, Var<T> y)
  {
    y.Value = x;
    return true;
  }

  public static bool Mod(int x, int y, Var<int> res)
  {
    res.Value = x % y;
    return true;
  }

  public static IEnumerable<bool> SmallPrimeFactor(int x, Var<int> p)
  {
    foreach (var r in SmallPrime_o(p))
    {
      if (!r) continue;
      var tmp = new Var<int>();
      if (!Mod(x, p, tmp)) continue;
      if (!Eq(tmp, 0)) continue;
      yield return true;
    }
  }

  public static void PrintSpf(int num)
  {
    Var<int> x = new();
    foreach (var p in SmallPrimeFactor(num, x))
    {
      Console.WriteLine(x.ToString());
    }

  }

  public static bool HasSmallPrimeFactor(int x)
  {
    var p = new Var<int>();
    foreach (var r in SmallPrime_o(p))
    {
      if (!r) continue;
      var tmp = new Var<int>();
      if (!Mod(x, p, tmp)) continue;
      if (Eq(tmp, 0)) return true;
    }
    return false;
  }
}
