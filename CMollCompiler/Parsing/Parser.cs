using Cmoll.Compiler.Core;
using Cmoll.Compiler.Scanning;

namespace Cmoll.Compiler.Parsing;

internal class Parser
{
  private CmcOptions options;
  private List<Token> tokens;
  CompilerState state;
  private int current = 0;
  private Token Peek => tokens[current];
  private Token Previous => current > 0 ? tokens[current - 1] : tokens[0];
  InputStatus CurrentInputStatus => Peek.Status;
  public Parser(CompilerState state, CmcOptions options, List<Token> tokens)
  {
    this.options = options;
    this.tokens = tokens;
    this.state = state;
  }
  private bool IsAtEnd()
  {
    var t = Peek.Type;
    return t == TokenEof || t == TokenError;
  }
  private bool Check(TokenType type)
  {
    if (IsAtEnd()) return false;
    return Peek.Type == type;
  }
  private Token Advance()
  {
    if (!IsAtEnd())
    {
      current++;
      var tok = Peek;
      if (tok.Type == TokenError)
        throw CmcException.Create(Invalid_token, tok.Status, tok.StringValue);
    }
    return Previous;
  }

  CmcException CreateTokenException(CmcErrorNumbers errno, Token tok)
    => CmcException.Create(errno, tok.Status, tok.StringValue);
  private bool Match(params TokenType[] types)
  {
    foreach (TokenType type in types)
    {
      if (Check(type))
      {
        Advance();
        return true;
      }
    }
    return false;
  }
  private Token Consume(TokenType type, string expected, InputStatus? status = null)
  {
    if (Check(type)) return Advance();
    var stat = status ?? Previous.Status;
    throw CmcException.Create(Unexpected_consume, stat, expected, Previous.StringValue);
  }
  public Expr Parse()
  {
    var t = SubTermList(";");
    return t;
  }

  object SingleTermItem(Token tok)
  {
    if (Match(TokenInt)) return new Number(tok.StringValue, new(typeof(int))) { Status = Previous.Status };
    if (Match(TokenFloat)) return new Number(tok.StringValue, new(typeof(double))) { Status = Previous.Status };
    if (Match(TokenOperator)) return tok;
    throw CreateTokenException(Unexpected_term_token, tok);
  }

  public Term SubTermList(string endTermS)
  {
    List<object> list = new();
    while (true)
    {
      var tok = Peek;
      if (tok.StringValue == endTermS) break;
      if (Match(TokenLeftParen))  // () sub term
      {
        var pTerm = SubTermList(")");
        pTerm.Status = tok.Status.Union(CurrentInputStatus);
        pTerm.Prio = 1;
        list.Add(pTerm);
        continue;
      }
      var st = SingleTermItem(tok);
      list.Add(st);
    }
    Advance();
    var term = BuildTerm(list);
    term.FixPrio();
    return term;
  }

  Term BuildTerm(List<object> list)
  {
    while (list.Count > 1)
      list = ReduceTermList(list);
    if (list.Count == 0) throw CreateTokenException(Invalid_token, Previous);
    var item = list[0];
    if (item is not Term term) throw CreateTokenException(Malformed_term, Previous);
    return term;
  }

  List<object> ReduceTermList(List<object> list)
  {
    if (list.Count <= 1) return list;
    var result = new List<object>();
    var (idx, oi) = state.OpTable.FindBestMatchOp(list);
    if (oi == null) throw CmcException.Create(Malformed_term, CurrentInputStatus);
    Term? arg0 = null, arg1 = null;
    if (oi.IsInfix || oi.IsPostfix)
    {
      result.AddRange(list[..(idx - 1)]);
      if (list[idx - 1] is not Term t) throw CmcException.Create(Malformed_term, CurrentInputStatus);
      arg0 = t;
    }
    int remIndex = idx + 1;
    if (oi.IsInfix || oi.IsPrefix)
    {
      if (list[idx + 1] is not Term t) throw CmcException.Create(Malformed_term, CurrentInputStatus);
      if (arg0 == null) arg0 = t; else arg1 = t;
      if (oi.IsInfix) remIndex++;
    }
    // check the arg prio
    int a0Prio = arg0!.Prio;
    int a1Prio = arg1?.Prio ?? 1;
    var dstStatus = arg0.Status.Union(arg1?.Status ?? InputStatus.Empty);
    dstStatus = dstStatus.Union(((Token)list[idx]).Status);
    if (a0Prio > oi.MaxPrioArg0 || a1Prio > oi.MaxPrioArg1) throw CmcException.Create(Malformed_term, dstStatus);
    // build the final Term
    var ot = new OpTerm(oi, arg0, arg1) { Status = dstStatus, Prio = oi.Priority };
    result.Add(ot);
    result.AddRange(list[remIndex..]);
    return result;
  }

}



