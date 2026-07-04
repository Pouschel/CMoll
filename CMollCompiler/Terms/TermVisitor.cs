using System;
using System.Collections.Generic;
using System.Text;
#pragma warning disable CS8603 // Possible null reference return.

namespace Cmoll.Compiler.Terms;

internal abstract class TermVisitor<T>
{

  public virtual T VisitNumberTerm(NumberTerm t) => throw CmcErrors.InternalCompilerError(this,t);
  public virtual T VisitStringTerm(StringTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitNameTerm(NameTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitParenTerm(ParenTerm t) => t.Inner.Accept(this);
  public virtual T VisitCallTerm(CallTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitOpTerm(OpTerm t) => throw CmcErrors.InternalCompilerError(this, t);
}
