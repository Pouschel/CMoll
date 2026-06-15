#nullable disable warnings

using System.Diagnostics;
using System.Globalization;
using System.Reflection;


namespace Testing.Framework;

public class Tester
{

	static int nTests, nOk, nFails;

	static Stopwatch sw;

	static ConsoleColor TestColor = ConsoleColor.Cyan,
		ErrorColor = ConsoleColor.Red,
		OkColor = ConsoleColor.Green,
		ResultColor = ConsoleColor.Yellow,
		DefaultColor = ConsoleColor.White;

	static HashSet<Assembly> testedAssemblies = new HashSet<Assembly>();

	static object lck = new object();
	public static bool Multithreaded = true;
	public static void Start()
	{
		nTests = nOk = nFails = 0;
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		DefaultColor = Console.ForegroundColor;
		testedAssemblies = new HashSet<Assembly>();
		if (Console.BackgroundColor == ConsoleColor.Black || Console.ForegroundColor == ConsoleColor.White)
		{
			TestColor = ConsoleColor.Cyan;
			ErrorColor = ConsoleColor.Red;
			OkColor = ConsoleColor.Green;
		}
		WriteLineWithColor("--- Start Testing ---", DefaultColor);
		sw = new Stopwatch();
		sw.Start();
	}
	public static void Register(bool passed)
	{
		Interlocked.Increment(ref nTests);
		if (passed) Interlocked.Increment(ref nOk);
		else Interlocked.Increment(ref nFails);
	}
	public static long DoTest(string name, Func<bool> testFunc)
	{
		int windowWidth = 70;
		if (!Console.IsOutputRedirected)
			windowWidth = Console.WindowWidth;
		var writeLock = typeof(Tester);
		Stopwatch sw = new Stopwatch();
		try
		{
			bool b = testFunc();
			Register(b);
			sw.Stop();
			if (!b)
			{
				Interlocked.Increment(ref nFails);
				WriteLineWithColor("", ErrorColor);
				WriteLineWithColor("Fail: " + name, ErrorColor);
			}
		}
		catch (Exception ex)
		{
			sw.Stop();
			var theEx = ex;
			if (ex is TargetInvocationException)
				theEx = ex.InnerException;
			Register(false);
			lock (writeLock)
			{
				DumpExecption(theEx, name);
			}
		}
		lock (writeLock)
		{
			string s = $"{name}: {sw.ElapsedMilliseconds} ms";
			int safe = 4;
			if (s.Length >= windowWidth - safe)
				s = "..." + s.Substring(s.Length - windowWidth + safe + 3);
			else
				s += new string(' ', windowWidth - safe - s.Length);
			s = "\r" + s;
			WriteWithColor(s, TestColor);
		}
		return sw.ElapsedMilliseconds;
	}

	static List<object[]> GetTestArguments(MethodInfo mi)
	{
		List<object[]> resList = new();
		bool hasTest = false;
		var miParams = mi.GetParameters();
		foreach (var attrib in mi.GetCustomAttributes())
		{
			var tname = attrib.GetType();
			if (tname == typeof(TestAttribute))
			{
				hasTest = true;
				continue;
			}
			if (attrib is TestCaseAttribute tca)
			{
				hasTest = true;
				var args = tca.Arguments;
				for (int i = 0; i < args.Length; i++)
				{
					var ai = args[i];
					if (ai.GetType() != miParams[i].ParameterType)
						args[i] = Convert.ChangeType(ai, miParams[i].ParameterType);
				}
				resList.Add(args);
			}
		}
		if (resList.Count == 0 && hasTest) resList.Add(Array.Empty<object>());
		return hasTest ? resList : null;
	}

	static object CreateInstance(Type t)
	{
		try
		{
			return Activator.CreateInstance(t);
		}
		catch
		{
			return null;
		}
	}

	public static void TestAssemblyWithType(Type type)
	{
		var asm = type.Assembly;
		TestAssembly(asm);
	}

	static void DumpExecption(Exception ex, string methodName)
	{
		lock (lck)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = ErrorColor;
			Console.WriteLine();
			Console.ForegroundColor = ErrorColor;
			Console.WriteLine($"{nFails}: {methodName}");
			Console.WriteLine(ex.GetType());
			Console.WriteLine(ex.Message);

			var stlines = ex.StackTrace.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < stlines.Length; i++)
			{
				var line = stlines[i];
				if (line.Contains("NUnit.Framework"))
					continue;
				Console.WriteLine(line);
			}
			Console.WriteLine();
			Console.ForegroundColor = oldColor;
		}
	}

	public static void TestAssembly(Assembly asm)
	{
		lock (testedAssemblies)
		{
			if (testedAssemblies.Contains(asm))
				return;
			testedAssemblies.Add(asm);
		}
		//WriteWithColor($"{asm.GetName().Name}", DefaultColor);
		//WriteLine();
		long maxTime = 0;
		string maxLine = "";
		object lck = new object();
		var types = asm.GetTypes();
		Parallel.For(0, types.Length, idx =>
		{
			var t = types[idx];
			//if (!IsTestFixture(t))
			//	return;
			var (time, line) = TestTypeWorker(t, lck);
			if (time > 0)
				lock (lck)
				{
					if (time > maxTime)
					{
						maxTime = time;
						maxLine = line;
					}
				}
		});
		lock (lck)
		{
			if (maxTime > 0)
			{
				WriteLineWithColor(maxLine, DefaultColor);
			}
		}
	}

	/// <summary>
	/// Runs the test for one type as it would be a complete test run.
	/// </summary>
	/// <param name="type">The type.</param>
	/// <param name="stopsOnFails"></param>
	public static void TestTypeFull(Type type, bool stopsOnFails = true)
	{
		Start();
		var (maxTime, maxLine) = TestTypeWorker(type, new object());
		if (maxTime > 0)
		{
			WriteWithColor(maxLine, DefaultColor);
		}
		WriteLineWithColor("", DefaultColor);
		End();
		if (stopsOnFails && nFails > 0)
		{
			Console.WriteLine("Press Enter to continue.");
			Console.ReadLine();
		}
	}

	public static void TestType(Type type)
	{
		var lck = new object();
		TestTypeWorker(type, lck);
	}

	public static (long maxTime, string maxLine) TestTypeWorker(Type type, object writeLock)
	{
		long maxTime = 0;
		string maxLine = null;
		int windowWidth = 70;
		if (!Console.IsOutputRedirected)
			windowWidth = Console.WindowWidth;
		foreach (var method in type.GetRuntimeMethods())
		{
			var testArgs = GetTestArguments(method);
			if (testArgs == null)
				continue;
			string methodName = type.FullName + "." + method.Name;
			Stopwatch sw = new Stopwatch();
			for (int i = 0; i < testArgs.Count; i++)
			{
				try
				{
					Interlocked.Increment(ref nTests);
					object? obj = null;
					if (!method.IsStatic)
						obj = CreateInstance(type);
					sw.Start();
					var mres = method.Invoke(obj, testArgs[i]);
					if (mres is Task task)
					{
						task.Wait();
					}
					sw.Stop();
					Interlocked.Increment(ref nOk);
				}
				catch (Exception ex)
				{
					sw.Stop();
					var theEx = ex;
					if (ex is TargetInvocationException)
						theEx = ex.InnerException;
					if (ex is AggregateException aex)
						theEx = ex.InnerException;
					Interlocked.Increment(ref nFails);
					lock (writeLock)
					{
						DumpExecption(theEx, methodName + $"@{i}");
					}
				}
			}
			string s = null;
			lock (writeLock)
			{
				s = $"{methodName}: {sw.ElapsedMilliseconds} ms";
				int safe = 4;
				if (s.Length >= windowWidth - safe)
					s = "..." + s.Substring(s.Length - windowWidth + safe + 3);
				else
					s += new string(' ', windowWidth - safe - s.Length);
				s = "\r" + s;
				WriteWithColor(s, TestColor);
			}
			if (sw.ElapsedMilliseconds > maxTime)
			{
				maxTime = sw.ElapsedMilliseconds;
				maxLine = s;
			}
		}
		return (maxTime, maxLine);
	}

	static void WriteHelperFunc(Action action, ConsoleColor color)
	{
		lock (lck)
		{
			var oldColor = Console.ForegroundColor;
			Console.ForegroundColor = color;
			action();
			Console.ForegroundColor = oldColor;
		}
	}

	static void WriteWithColor(string text, ConsoleColor color) => WriteHelperFunc(() => Console.Write(text), color);

	static void WriteLineWithColor(string text, ConsoleColor color) => WriteHelperFunc(() => Console.WriteLine(text), color);

	public static bool End()
	{
		sw.Stop();
		WriteLineWithColor("", DefaultColor);
		WriteLineWithColor("", DefaultColor);
		WriteWithColor("Result   : ", ResultColor);

		if (nFails == 0)
			WriteWithColor("All tests passed!", OkColor);
		else
			WriteWithColor("Failed!", ErrorColor);

		WriteLineWithColor("", DefaultColor);

		WriteWithColor("Tests run: ", ResultColor);
		WriteWithColor(nTests.ToString(), DefaultColor);
		WriteWithColor(", Passed: ", ResultColor);
		WriteWithColor(nOk.ToString(), OkColor);
		WriteWithColor(", Failed: ", ResultColor);
		WriteLineWithColor(nFails.ToString(), nFails == 0 ? OkColor : ErrorColor);

		WriteWithColor("End time : ", ResultColor);
		WriteLineWithColor(DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"), DefaultColor);

		WriteWithColor("Duration : ", ResultColor);
		WriteLineWithColor($"{sw.ElapsedMilliseconds / 1000.0:f2} s", DefaultColor);

		WriteLineWithColor("--- End Testing ---", DefaultColor);
		return nFails == 0;
	}

}
