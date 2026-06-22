using Cmoll.Compiler.Terms;

namespace CmollTester;

class TermTester
{
  static string OkTerms = @"
1+2    : + 
3 * 4  : * 
1+2+3  : + 
1+2*3  : +
(1+2)*3 : *
";

  static string ErrTerms = @"
1+
";

  public static Term? CompileOpTerm(string termCode, string endTermSymbol = ".")
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, termCode + endTermSymbol);
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);
    return parser.SubTermList(endTermSymbol);
  }

  public static Term? CheckOkTerm(string termCode, OperatorInfo expectedOp, string endTermSymbol = ".")
  {
    var term = CompileOpTerm(termCode, endTermSymbol)!;
    term.NotNullExpected();
    if (term is not OpTerm ot)
    {
      Fail(); return null;
    }
    ot.op.EqualExpected(expectedOp);
    return term;
  }

  public static bool CheckErrorTerm(string termCode, string endTermSymbol = ".")
  {
    Throws<CmcException>(() => CompileOpTerm(termCode,endTermSymbol));
    return true;
  }

  public static void CheckTermList(string list, bool ok)
  {
    var cstate = new CompilerState();
    var lines = list.Split('\n');
    foreach (var line in lines)
    {
      var l = line.Trim();
      if (string.IsNullOrEmpty(l)) continue;

      if (ok)
      {
        var arity = 2;
        var parts = l.Split(':');
        if (parts.Length == 3)
          arity = int.Parse(parts[2].Trim());
        var opinfo = cstate.OpTable.GetWithArity(parts[1].Trim(), arity);
        Tester.DoTest(l, () => CheckOkTerm(parts[0], opinfo!) != null);
      }
      else Tester.DoTest(l, () => CheckErrorTerm(l));
    }
  }


  public static void TestAllTerms()
  {
    CheckTermList(OkTerms, true); 
    CheckTermList(ErrTerms, false);
  }
}
