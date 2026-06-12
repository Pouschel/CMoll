using System;
using System.Collections.Generic;
using System.Text;
using Cmoll.Compiler.Scanning;
using CMoll.Compiler;

namespace Cmoll.Compiler.Parsing;

internal class Parser
{
  private CmcOptions options;
  private List<Token> tokens;

  public Parser(CmcOptions options, List<Token> tokens)
  {
    this.options = options;
    this.tokens = tokens;
  }
}

abstract class ExprStmtBase
{
  public InputStatus Status = InputStatus.Empty;
  public override string ToString()
    => Status.IsEmpty ? Status.ReadPartialText() : $"a {this.GetType().Name}";
}
