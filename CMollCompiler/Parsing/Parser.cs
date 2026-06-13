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
    var t = Term();
    return t;
  }

  Term Term()
  {
    var tok = Peek;
    if (Match(TokenLeftParen))
    {
      var expr = Term();
      Consume(TokenRightParen, ")");
      expr.Status = tok.Status.Union(Previous.Status);
      return TermRhs(expr);
    }
    if (Match(TokenInt)) return TermRhs(new Number(tok.StringValue, new(typeof(int))));
    if (Match(TokenFloat)) return TermRhs(new Number(tok.StringValue, new(typeof(double))));
    throw CreateTokenException(Unexpected_term_token, tok);

  }

  Term TermRhs(Term lhs)
  {
    var tok = Peek;
    if (tok.Type != TokenOperator) return lhs;
    var op = state.OpTable.GetRhsOperator(tok.StringValue);
    // this should an error, because we have no operators of the form xf or yf
    if (op == null || op.Arity == 1) throw CreateTokenException(Invalid_token, tok);
    var rhs = Term();
    return new OpTerm(op, lhs, rhs) { Prio = op.Priority };
  }

}



