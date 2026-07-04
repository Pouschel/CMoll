using System;
using System.Collections.Generic;
using System.Text;
#pragma warning disable CS8603 // Possible null reference return.

namespace Cmoll.Compiler.Terms;

internal abstract class TermVisitor<T>
{

  public virtual T VisitNumberTerm(NumberTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitStringTerm(StringTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitNameTerm(NameTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitParenTerm(ParenTerm t) => t.Inner.Accept(this);
  public virtual T VisitCallTerm(CallTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitOpTerm(OpTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public virtual T VisitClauseTerm(ClauseTerm t) => throw CmcErrors.InternalCompilerError(this, t);
  public void VisitTerms(List<Term> terms)
  {
    foreach (var t in terms)
    {
      t.Accept(this);
    }
  }
}

internal abstract class TermTransformer : TermVisitor<Term>
{
  public override Term VisitCallTerm(CallTerm t) => t;
  public override Term VisitNameTerm(NameTerm t) => t;
  public override Term VisitNumberTerm(NumberTerm t) => t;
  public override Term VisitOpTerm(OpTerm t) => t;
  public override Term VisitParenTerm(ParenTerm t) => t;
  public override Term VisitStringTerm(StringTerm t) => t;
  public override Term VisitClauseTerm(ClauseTerm t) => t;

  public void TransformTerms(IList<Term> terms)
  {
    for (int i = 0; i < terms.Count; i++)
    {
      terms[i] = terms[i].Accept(this);
    }
  }
}
