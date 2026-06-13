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
    return Term();
  }

  Term Term()
  {
    var tok = Peek;
    if (Match(TokenLeftParen))
    {
      var expr = Term();
      Consume(TokenRightParen, ")");
      expr.Status = tok.Status.Union(Previous.Status);
      return expr;
    }
    return null;
  }

}

abstract record Expr
{
  public InputStatus Status = InputStatus.Empty;
  public override string ToString()
    => Status.IsEmpty ? Status.ReadPartialText() : $"a {this.GetType().Name}";
}

record Term : Expr
{ }

record Literal : Term
{ }

record ModulExpr(string Name) : Expr
{

}
