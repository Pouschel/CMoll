
namespace Testing.Framework;

[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Class)]
public class TestFixtureAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TestCaseAttribute : Attribute
{
	public object[] Arguments;

	public TestCaseAttribute(params object[] args)
	{
		this.Arguments = args;
	}

}
