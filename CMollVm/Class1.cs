namespace CMollVm;

public struct Reg
{
  public long IVal;
  public string SVal;

}

public class VM
{
  public Reg[] Regs;

  public List<long> IMem = [];
  public List<string> SMem = [];

  public List<Reg> Stack = [];
  public VM()
  {
    Regs = new Reg[8];
  }

  // mov ir0 #32
  // data ivals: 8 7 5
  // mov ir1 ivals[1]
}
