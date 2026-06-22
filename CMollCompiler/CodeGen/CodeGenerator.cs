using System;
using System.CodeDom.Compiler;
using Cmoll.Compiler.Terms;

namespace Cmoll.Compiler.CodeGen;

internal class CodeGenerator
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
  public void Emit(string code) => tw.Write(code);

  public void GenCode(List<Term> terms)
  {
    foreach (var t in terms)
    {
      t.GenCode(this);
    }
    CloseAll();
  }
}

static class Term2Code
{
  public static void GenCode(this Number t, CodeGenerator cgen) => cgen.Emit(t.Value);

  public static void GenCode(this Name t, CodeGenerator cgen) => cgen.Emit(t.Text);

  public static bool GenCode(this OpTerm t, CodeGenerator cgen)
  {
    switch (t.op.Symbol)
    {
      case ":-":
        t.arg0.GenCode(cgen);
        break;
      case "module":
        cgen.CloseAll();
        cgen.Emit($"public static partial class ");
        t.arg0.GenCode(cgen);
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
      case Name name: name.GenCode(cgen); break;
      case Number n: n.GenCode(cgen); break;
      case OpTerm ot: ot.GenCode(cgen); break;
      default: throw new NotImplementedException();
    }

  }

}

