namespace CmollTester;

class TermTester
{
  public static string OkTerms = @"
1+2
3 * 4
1+2+3
";

  public static Term? CheckOkTerm(string termCode, string endTermSymbol = ";")
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, termCode + endTermSymbol);
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);
    var term = parser.SubTermList(endTermSymbol);
    term.NotNullExpected();
    return term;
  }

  public static void CheckErrorTerm(string termCode, string endTermSymbol = ";")
  {
    var cstate = new CompilerState();
    Scanner scan = new Scanner(cstate, termCode + endTermSymbol);
    var tokens = scan.ScanAllTokens();
    var parser = new Parser(cstate, new CmcOptions(), tokens);
    Throws<CmcException>(() => parser.SubTermList(endTermSymbol));
  }

  public static void CheckTermList(string list, bool ok)
  {
    var lines = list.Split('\n');
    foreach (var line in lines)
    {
      var l = line.Trim();
      if (string.IsNullOrEmpty(l)) continue;
      if (ok) Tester.DoTest(l, () =>  CheckOkTerm(l)!=null );
      
    }
  }


  public static void TestAllTerms()
  {
    CheckTermList(OkTerms, true);
  }
}
