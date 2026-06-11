



class Utils
{
  [DllImport("kernel32.dll")]
  public extern static void ExitProcess(long code);

  public static void Error(int code)
  {
    ExitProcess(code);
  }


}
