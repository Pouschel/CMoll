namespace Cmoll.Compiler.Scanning;

public struct Token
{
  public readonly TokenType Type { get; init; }
  public readonly InputStatus Status { get; init; }
  public readonly string StringValue { get; init; }
  public override readonly string ToString() => $"{Type}: {StringValue}";
  internal Token ChangeType(TokenType newType) => new() { Status = this.Status, Type = newType, StringValue = this.StringValue };

}

public enum TokenType
{
  // Single-character tokens.
  TokenLeftParen, TokenRightParen, // ()
  TokenLeftBrace, TokenRightBrace, // {}
  TokenLeftBracket, TokenRightBracket, //[]
  TokenDot,
  //TokenComma, TokenSemicolon,
  // Arithmetic op tokens
  TokenOperator,

  // Literals.
  TokenName, TokenString, TokenInt, TokenFloat,

  TokenComment,
  // Error, EOF
  TokenError, TokenEof
}
