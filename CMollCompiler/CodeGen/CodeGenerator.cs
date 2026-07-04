using System;
using System.CodeDom.Compiler;
using Cmoll.Compiler.Terms;

namespace Cmoll.Compiler.CodeGen;

internal class CodeGenerator: TermVisitor<bool>
{
  public IndentedTextWriter tw;

  Stack<(string, bool)> openUnits = [];

  public CodeGenerator(TextWriter tw)
  {
    this.tw = new(tw, "  ");
  }

  public void Open(string start, string end, bool indentAfter = true)
  {
    tw.WriteLine(start);
    if (indentAfter)
      tw.Indent++;
    openUnits.Push((end, indentAfter));
  }
  public void OpenBrace() => Open("{", "}", true);
  public void Close()
  {
    var (s, b) = openUnits.Pop();
    if (b) tw.Indent--;
    tw.WriteLine(s);
  }
  public void CloseAll()
  {
    while (openUnits.Count > 0) Close();
  }
  public void EmitLine(string code = "") => tw.WriteLine(code);
  public bool Emit(string code)
  {
    tw.Write(code); return true;
  }

  public void GenCode(List<Term> terms)
  {
    foreach (var t in terms)
    {
      t.Accept(this);
    }
    CloseAll();
  }

  public override bool VisitNumberTerm(NumberTerm t) => this.Emit(t.Value);

  public override bool VisitNameTerm(NameTerm t) => this.Emit(t.Text);
  public override bool VisitOpTerm(OpTerm t)
  {
    switch (t.Op.Symbol)
    {
      case ":-":
        t.Arg0.Accept(this);
        break;
      case "module":
        CloseAll();
        Emit($"public static partial class ");
        t.Arg0.Accept(this);
        EmitLine();
        OpenBrace();
        break;
      default: throw new NotImplementedException();
    }
    return true;
  }
  //public override bool VisitCallTerm(CallTerm t) => throw new NotImplementedException();
  //public override bool VisitStringTerm(StringTerm t) => throw new NotImplementedException();

}

static class Term2Code
{
  public static void GenCode(this NumberTerm t, CodeGenerator cgen) => cgen.Emit(t.Value);

  public static void GenCode(this NameTerm t, CodeGenerator cgen) => cgen.Emit(t.Text);

  public static bool GenCode(this OpTerm t, CodeGenerator cgen)
  {
    switch (t.Op.Symbol)
    {
      case ":-":
        t.Arg0.GenCode(cgen);
        break;
      case "module":
        cgen.CloseAll();
        cgen.Emit($"public static partial class ");
        t.Arg0.GenCode(cgen);
        cgen.EmitLine();
        cgen.OpenBrace();
        break;
      default: throw new NotImplementedException();
    }
    return true;
  }

  public static void GenCode(this Term t, CodeGenerator cgen)
  {
    switch (t)
    {
      case NameTerm name: name.GenCode(cgen); break;
      case NumberTerm n: n.GenCode(cgen); break;
      case OpTerm ot: ot.GenCode(cgen); break;
      default: throw CmcErrors.CodegenError(t);
    }

  }

}

