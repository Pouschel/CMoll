#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Testing.Framework;

public static class Assert
{

	/// <summary>
	/// Asserts that a condition is true. If the condition is false the method throws
	/// an <see cref="AssertExecption"/>.
	/// </summary>
	/// <param name="condition">The evaluated condition</param>
	public static void IsTrue(bool condition)
	{
		if (!condition)
			throw new AssertExecption("IsTrue failed");
	}

	public static void TrueExpected(this bool condition) => IsTrue(condition);
	public static void FalseExpected(this bool condition) => IsFalse(condition);

	public static void EqualExpected(this object? o, object? other) => AreEqual(other, o);
	public static void NotEqualExpected(this object o, object other) => AreNotEqual(other, o);
	public static void EqualExpected(this double a, double b, double tol) => AreEqual(a, b, tol);
	public static void NotNullExpected(this object o) => NotNull(o);
	public static void NullExpected(this object? o) => IsNull(o);
	public static void ZeroExpected(this object o) => AreEqual(o, 0);
	public static void OneExpected(this object o) => AreEqual(o, 1);
	public static void ExpectThrow<T>(this Action action) where T : Exception => Throws<T>(action);

	/// <summary>
	/// Asserts that a condition is false. If the condition is true the method throws
	/// an <see cref="AssertExecption"/>.
	/// </summary> 
	/// <param name="condition">The evaluated condition</param>
	public static void IsFalse(bool condition)
	{
		if (condition)
			throw new AssertExecption("IsFalse failed");
	}
	public static void Fail() => throw new AssertExecption("Fail");
	private static bool DoAreEqual(object? expected, object? actual)
	{
		if (ReferenceEquals(expected, actual))
			return true;
		if (expected is null)
			return false;
		if (expected.Equals(actual))
			return true;
		if (actual is null) 
			return false;
		if (actual.Equals(expected))
			return true;

		if (expected is IEquatable<object> eq)
			return eq.Equals(actual);
		if (actual is IEquatable<object> eq2)
			return eq2.Equals(expected);

		// now try conversions
		if (expected is not IConvertible && actual is not IConvertible)
			return false;
		try
		{
			IConvertible exc = (IConvertible)expected;
			IConvertible acc = (IConvertible)actual;
			var exTypeCode = exc.GetTypeCode();
			var acTypeCode = acc.GetTypeCode();
			// simply take the maximum for now
			var maxType = exc.GetType();
			if (acTypeCode > exTypeCode)
				maxType = acc.GetType();
			expected = exc.ToType(maxType, CultureInfo.InvariantCulture);
			actual = acc.ToType(maxType, CultureInfo.InvariantCulture);
			return expected.Equals(actual);
		}
		catch (Exception)
		{ }
		return false;
	}

	/// <summary>
	/// Verifies that two objects are equal.  Two objects are considered
	/// equal if both are null, or if both have the same value. NUnit
	/// has special semantics for some object types.
	/// If they are not equal an <see cref="AssertExecption"/> is thrown.
	/// </summary>
	/// <param name="expected">The value that is expected</param>
	/// <param name="actual">The actual value</param>
	public static void AreEqual(object expected, object actual, string _="")
	{
		if (!DoAreEqual(expected, actual))
			throw new AssertExecption(expected, actual);
	}

	//private static bool DoAreEqual<T>(T expected, T actual)
	//{
	//	if (ReferenceEquals(expected, actual))
	//		return true;
	//	if (expected is null)
	//		return false;
	//	if (expected is IEquatable<T> e1)
	//		return e1.Equals(actual);
	//	return EqualityComparer<T>.Default.Equals(expected, actual);
	//}

	public static void AreEqual<T>(T expected, T actual)
	{
		if (!DoAreEqual(expected, actual) && !EqualityComparer<T>.Default.Equals(expected, actual))
			throw new AssertExecption(expected, actual);
	}

	/// <summary>
	/// Verifies that two objects are not equal.  Two objects are considered
	/// equal if both are null, or if both have the same value. NUnit
	/// has special semantics for some object types.
	/// If they are equal an <see cref="AssertExecption"/> is thrown.
	/// </summary>
	/// <param name="expected">The value that is expected</param>
	/// <param name="actual">The actual value</param>
	public static void AreNotEqual(object expected, object actual)
	{
		if (DoAreEqual(expected, actual))
			throw new AssertExecption(string.Format("Values should not be equal: {0}", expected));
	}

	public static void AreNotEqual<T>(IEquatable<T> expected, T actual)
	{
		if (DoAreEqual(expected, actual))
			throw new AssertExecption(string.Format("Values should not be equal: {0}", expected));
	}

	/// <summary>
	/// Verifies that two doubles are equal considering a delta. If the
	/// expected value is infinity then the delta value is ignored. If 
	/// they are not equal then an <see cref="AssertExecption"/> is
	/// thrown.
	/// </summary>
	/// <param name="expected">The expected value</param>
	/// <param name="actual">The actual value</param>
	/// <param name="delta">The maximum acceptable difference between the
	/// the expected and the actual</param>
	public static void AreEqual(double expected, double actual, double delta)
	{
		double curDelta = Math.Abs(expected - actual);
		if (curDelta > delta)
			throw new AssertExecption(expected, actual, curDelta);
	}

	/// <summary>
	/// Verifies that the object that is passed in is equal to <code>null</code>
	/// If the object is not <code>null</code> then an <see cref="AssertExecption"/>
	/// is thrown.
	/// </summary>
	/// <param name="anObject">The object that is to be tested</param>
	public static void IsNull(object anObject)
	{
		if (anObject is null)
			return;
		throw new AssertExecption("IsNull failed");
	}

	public static void NotNull(object anObject)
	{
		if (!(anObject is null))
			return;
		throw new AssertExecption("NotNull failed");
	}

	/// <summary>
	/// Verifies that the object that is passed in is not equal to <code>null</code>
	/// If the object is <code>null</code> then an <see cref="AssertExecption"/>
	/// is thrown.
	/// </summary>
	/// <param name="anObject">The object that is to be tested</param>
	public static void IsNotNull(object? anObject)
	{
		if (ReferenceEquals(anObject, null))
			throw new AssertExecption("IsNotNull failed");
	}

	/// <summary>
	/// Verifies that the double that is passed in is an <code>NaN</code> value.
	/// If the object is not <code>NaN</code> then an <see cref="AssertExecption"/>
	/// is thrown.
	/// </summary>
	/// <param name="d">The value that is to be tested</param>
	public static void IsNaN(double d)
	{
		if (!double.IsNaN(d))
			throw new AssertExecption(string.Format("{0} is not NaN", d));
	}

	/// <summary>
	/// Verifies that a delegate throws a particular exception when called.
	/// </summary>
	/// <typeparam name="ExType">Type of the expected exception</typeparam>
	/// <param name="code">A TestDelegate</param>
	public static ExType Throws<ExType>(Action code) where ExType : Exception
	{
		try
		{
			code();
		}
		catch (ExType ex)
		{
			return ex;
		}
		catch (Exception)
		{ }
		throw new AssertExecption(string.Format("{0} exception expected.", typeof(ExType)));
	}

	public static void Fail(string msg)
	{
		throw new AssertExecption(msg);
	}

}

/// <summary>
/// Thrown when an assertion failed.
/// </summary>
class AssertExecption : Exception
{
	public AssertExecption(string msg) : base(msg)
	{

	}

	public AssertExecption(object expected, object actual) : this(string.Format(@"Expected: {0}
Actual  : {1}", expected, actual))
	{
	}

	public AssertExecption(double expected, double actual, double delta)
		: this(string.Format(@"Expected: {0}
Actual  : {1}
Delta   : {2}", expected, actual, delta))
	{
	}

}
