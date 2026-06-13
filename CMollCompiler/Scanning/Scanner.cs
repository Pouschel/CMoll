using Cmoll.Compiler.Core;
using static System.Net.Mime.MediaTypeNames;

namespace Cmoll.Compiler.Scanning;

internal ref struct Scanner
{
  int start;
  int current;
  private readonly string fileName;
  private readonly string source;
  int line, col, startLine, startCol;
  CompilerState state;
  public Scanner(CompilerState state, string source, string fileName = "")
  {
    this.state = state;
    this.fileName = fileName;
    this.source = source;
    this.start = this.current = 0;
    this.line = this.col = 1;
    this.startLine = 1; this.startCol = 1;
  }

  private Token ScanToken()
  {
    SkipWhitespace();
    start = current;
    startLine = line; startCol = col;
    if (IsAtEnd) return MakeToken(TokenEof);
    char c = Advance();
    if (IsAlpha(c)) return Identifier();
    if (IsDigit(c)) return Number();
    var tok = c switch
    {
      '(' => MakeToken(TokenLeftParen),
      ')' => MakeToken(TokenRightParen),
      '{' => MakeToken(TokenLeftBrace),
      '}' => MakeToken(TokenRightBrace),
      '[' => MakeToken(TokenLeftBracket),
      ']' => MakeToken(TokenRightBracket),
      ';' => MakeToken(TokenSemicolon),
      ',' => MakeToken(TokenComma),
      '/' => Match('/') ? Comment() : OperatorOrError(),
      '"' => ScanString(),
      _ => OpChars.Contains(c) ? OperatorOrError() : ErrorToken($"Unexpected char: '{c}'")
    };
    return tok;
  }
  static string OpChars = "+-*/=<>!%.";

  Token OperatorOrError()
  {
    while (true)
    {
      char c = Peek();
      if (!OpChars.Contains(c)) break;
      Advance();
    }
    var tok = MakeToken(TokenOperator);
    if (!state.OpTable.ContainsOperator(tok.StringValue))
      tok = ErrorToken($"Invalid operator '{tok.StringValue}'");
    return tok;
  }


  Token Comment()
  {
    // A comment goes until the end of the line.
    while (Peek() != '\n' && Peek() != '\r' && !IsAtEnd) Advance();
    return MakeToken(TokenComment);
  }

  public List<Token> ScanAllTokens(bool ignoreComments = true)
  {
    var result = new List<Token>();
    while (true)
    {
      var token = ScanToken();
      if (ignoreComments && token.Type == TokenComment) continue;
      result.Add(token);
      if (token.Type == TokenEof || token.Type == TokenError) break;
    }
    return result;
  }
  static bool IsDigit(char c) => c >= '0' && c <= '9';
  static bool IsAlpha(char c) => char.IsLetter(c) || c == '_';
  Token Number()
  {
    while (IsDigit(Peek())) Advance();
    // Look for a fractional part.
    if (Peek() == '.' && IsDigit(Peek(1)))
    {
      // Consume the ".".
      Advance();
      while (IsDigit(Peek())) Advance();
    }
    return MakeToken(TokenNumber);
  }
  Token Identifier()
  {
    while (IsAlpha(Peek()) || IsDigit(Peek()))
      Advance();
    return MakeToken(TokenName);
  }
  Token ScanString()
  {
    while (Peek() != '"' && !IsAtEnd)
    {
      if (Peek() == '\n') { line++; col = 0; }
      Advance();
    }
    if (IsAtEnd)
      return ErrorToken("Nicht abgeschlossene Zeichenkette.");

    // The closing quote.
    Advance();
    return MakeToken(TokenString);
  }
  void SkipWhitespace()
  {
    while (true)
    {
      char c = Peek();
      switch (c)
      {
        case ' ':
        case '\r':
        case '\t':
          Advance();
          break;
        case '\n':
          line++;
          col = 0;
          Advance();
          break;
        default:
          if (char.IsWhiteSpace(c))
            continue;
          return;
      }
    }
  }

  char Advance()
  {
    col++;
    return source[current++];
  }
  char Peek(int n = 0) => current >= source.Length - n ? '\0' : source[current + n];
  char PeekFromStart(int n = 0) => start >= source.Length - n ? '\0' : source[start + n];
  bool IsAtEnd => current >= source.Length;

  bool Match(char expected)
  {
    if (IsAtEnd) return false;
    if (source[current] != expected) return false;
    current++;
    return true;
  }

  InputStatus CreateStatus()
  {
    return new InputStatus(fileName)
    {
      LineStart = startLine,
      LineEnd = line,
      ColStart = startCol, // col - (current - start),
      ColEnd = col
    };
  }

  Token MakeToken(TokenType type)
  {
    Token token = new()
    {
      Type = type,
      StringValue = source[start..current],
      Status = CreateStatus()
    };
    return token;
  }
  Token ErrorToken(string message)
  {
    Token token = new()
    {
      Type = TokenError,
      StringValue = message,
      Status = CreateStatus()
    };
    return token;
  }
}




