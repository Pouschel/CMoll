using Cmoll.Compiler.Core;
using Cmoll.Compiler.Scanning;
using CMoll.Compiler;

namespace Cmoll.Compiler.Parsing;

internal class Parser
{
  private CmcOptions options;
  private List<Token> tokens;
  CompilerState state;
  public Parser(CompilerState state, CmcOptions options, List<Token> tokens)
  {
    this.options = options;
    this.tokens = tokens;
    this.state = state;
  }

  public ExprBase Parse()
  {
    return null;
  }


}

abstract record ExprBase
{
  public InputStatus Status = InputStatus.Empty;
  public override string ToString()
    => Status.IsEmpty ? Status.ReadPartialText() : $"a {this.GetType().Name}";
}
