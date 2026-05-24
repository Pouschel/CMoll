using System.Diagnostics;

namespace CmRunner;


class ToolException(string msg) : Exception(msg)
{

}

internal class Program
{
  static string ToolDir = @"C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231\bin\Hostx64\x64";
  static string LibDir = @"C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\um\x64";

  static void WriteColor(ConsoleColor color, string text)
  {
    var col = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = col;
  }

  static void WriteError(string message) => WriteColor(ConsoleColor.Red, message);

  static int ExecuteProcess(string name, string args, string workingDir)
  {
    var pi = new ProcessStartInfo()
    {
      Arguments = args,
      FileName = name,
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      RedirectStandardInput = true,
      CreateNoWindow = true,
      WorkingDirectory = workingDir,
    };
    var proc = Process.Start(pi);
    if (proc == null) return -1;

    string errout = proc.StandardError.ReadToEnd();
    string outout = proc.StandardOutput.ReadToEnd();
    Console.Write(errout);
    Console.Write(outout);
    proc.WaitForExit();
    return proc.ExitCode;
  }

  public static int AsmAndRun(string workDir, string fileName)
  {
    var masm = Path.Combine(ToolDir, "ml64.exe");
    var args = $"/c /Zi {fileName}.asm";
    if (ExecuteProcess(masm, args, workDir) != 0)
      throw new ToolException("Asm error");
    var link = Path.Combine(ToolDir, "link.exe");
    var sw = File.CreateText(Path.Combine(workDir, "link.txt"));
    sw.WriteLine("/debug"); sw.WriteLine("/ENTRY:main");
    sw.WriteLine($"/LIBPATH:\"{LibDir}\"");
    sw.WriteLine($"kernel32.lib");
    sw.WriteLine($"{fileName}.obj");
    sw.Close();
    args = "@link.txt";
    if (ExecuteProcess(link, args, workDir) != 0)
      throw new ToolException("Link error");

    var result = ExecuteProcess(Path.Combine(workDir, fileName + ".exe"), "", workDir);
    return result;
  }

  static void Main(string[] args)
  {
    Console.WriteLine("CMoll runner");
    var fn = @"C:\Code\AsmTest\t\t3_1.asm";
    var fnBase = Path.GetFileNameWithoutExtension(fn);
    string outDir = @$"R:\{fnBase}";
    if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
    var outName = Path.Combine(outDir, fnBase + ".asm");
    File.Copy(fn, outName, true);
    var result = AsmAndRun(outDir, fnBase);
    Console.WriteLine($"Program returned: {result}");
  }
}
