using System;
using System.Collections.Generic;
using System.Text;

namespace Cmoll.Compiler.Core;

internal record BaseType
{
  protected BaseType() { }
  public static BaseType NotResolved = new();
}


internal record CsType(Type Type): BaseType
{
}

