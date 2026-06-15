using System;
using System.Collections.Generic;
using System.Text;

namespace Cmoll.Compiler.Core;

internal record BaseType
{
  protected BaseType() { }
  public static BaseType NotResolved = new();

  public override string ToString() => this == NotResolved ? "(unrevolved)" : $"a {this.GetType().Name}";
}


internal record CsType(Type Type): BaseType
{
  public override string ToString() => Type.Name;

}

