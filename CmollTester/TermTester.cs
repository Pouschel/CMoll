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
1,2,3   : ,
1 * +2  : *
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
    if (!termCode.TrimEnd().EndsWith(endTermSymbol)) termCode += endTermSymbol;
    var term = CompileOpTerm(termCode, endTermSymbol)!;
    term.NotNullExpected();
    if (term is not OpTerm ot)
    {
      Fail(); return null;
    }
    ot.Op.EqualExpected(expectedOp);
    return term;
  }

  public static Term? CheckOkTerm(string termCode, string opTerm, string endTermSymbol = ".")
  {
    var term = CompileOpTerm(termCode, endTermSymbol)!;
    term.NotNullExpected();
    if (term is not OpTerm ot)
    {
      Fail(); return null;
    }
    ot.Op.Symbol.EqualExpected(opTerm);
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
        var parts = l.Split(':');
        var opt = parts[1].Trim();
        Tester.DoTest(l, () => CheckOkTerm(parts[0], opt) != null);
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
