namespace Cmoll.Compiler.Scanning;

public struct Token
{
  public readonly TokenType Type { get; init; }
  public readonly InputStatus Status { get; init; }
  public readonly string StringValue { get; init; }
  public override readonly string ToString() => $"{Type}: {StringValue}";

}

public enum TokenType
{
  // Single-character tokens.
  TokenLeftParen, TokenRightParen, // ()
  TokenLeftBrace, TokenRightBrace, // {}
  TokenLeftBracket, TokenRightBracket, //[]
  TokenComma, TokenSemicolon,
  // Arithmetic op tokens
  TokenDot, TokenMinus, TokenPlus,    // . - + 
  TokenSlash, TokenStar, // / *
  TokenPercent, // %
                // Comparison tokens
  TokenBang, TokenBangEqual, // !, !=
  TokenEqual, TokenEqualEqual,
  TokenGreater, TokenGreaterEqual,
  TokenLess, TokenLessEqual,
  // Literals.
  TokenIdentifier, TokenString, TokenNumber,
  
  TokenComment,
  // Error, EOF
  TokenError, TokenEof
}
