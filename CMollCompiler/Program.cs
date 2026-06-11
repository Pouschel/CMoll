

using System;

class Program
{
  static void Main(string[] args)
  {
    var fn = @"C:\Users\ts\Documents\Programmierunterstützung\bin\RunHelloDebug.dll";
    var retval= CsRunner.RunEntryPoint(fn, true);
  }
}

