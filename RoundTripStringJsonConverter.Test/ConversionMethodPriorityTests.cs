// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ConversionMethodPriorityTests
{
	/// <summary>
	/// Class with multiple conversion methods to test priority order.
	/// FromString should be selected over Parse.
	/// </summary>
	public class ClassWithMultipleMethods(string value)
	{
		public string Value { get; } = value;

		// This should be selected (highest priority)
		public static ClassWithMultipleMethods FromString(string value) => new($"FromString:{value}");

		// This should be ignored (lower priority)
		public static ClassWithMultipleMethods Parse(string value) => new($"Parse:{value}");

		public override string ToString() => Value;
	}

	/// <summary>
	/// Class with Parse and Create methods to test Parse priority over Create.
	/// </summary>
	public class ClassWithParseAndCreate(string value)
	{
		public string Value { get; } = value;

		// This should be selected (higher priority than Create)
		public static ClassWithParseAndCreate Parse(string value) => new($"Parse:{value}");

		// This should be ignored (lower priority)
		public static ClassWithParseAndCreate Create(string value) => new($"Create:{value}");

		public override string ToString() => Value;
	}

	/// <summary>
	/// Class with Create and Convert methods to test Create priority over Convert.
	/// </summary>
	public class ClassWithCreateAndConvert(string value)
	{
		public string Value { get; } = value;

		// This should be selected (higher priority than Convert)
		public static ClassWithCreateAndConvert Create(string value) => new($"Create:{value}");

		// This should be ignored (lower priority)
		public static ClassWithCreateAndConvert Convert(string value) => new($"Convert:{value}");

		public override string ToString() => Value;
	}

	private static JsonSerializerOptions GetOptions()
	{
		return new JsonSerializerOptions
		{
			Converters = { new RoundTripStringJsonConverterFactory() }
		};
	}

	[TestMethod]
	public void FromString_Should_Be_Prioritized_Over_Parse()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"test\"";

		ClassWithMultipleMethods? result = JsonSerializer.Deserialize<ClassWithMultipleMethods>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual("FromString:test", result.Value, "FromString method should be used over Parse");
	}

	[TestMethod]
	public void Parse_Should_Be_Prioritized_Over_Create()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"test\"";

		ClassWithParseAndCreate? result = JsonSerializer.Deserialize<ClassWithParseAndCreate>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual("Parse:test", result.Value, "Parse method should be used over Create");
	}

	[TestMethod]
	public void Create_Should_Be_Prioritized_Over_Convert()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"test\"";

		ClassWithCreateAndConvert? result = JsonSerializer.Deserialize<ClassWithCreateAndConvert>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual("Create:test", result.Value, "Create method should be used over Convert");
	}

	[TestMethod]
	public void Factory_Should_Detect_Convertible_Types_With_Multiple_Methods()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsTrue(factory.CanConvert(typeof(ClassWithMultipleMethods)));
		Assert.IsTrue(factory.CanConvert(typeof(ClassWithParseAndCreate)));
		Assert.IsTrue(factory.CanConvert(typeof(ClassWithCreateAndConvert)));
	}

	/// <summary>
	/// Generic class whose FromString returns a different closed type, so only Parse can produce it.
	/// </summary>
	public sealed class BoxWithMismatchedFromString<TValue>(string raw)
	{
		public string Raw { get; } = raw;

		public static BoxWithMismatchedFromString<string> FromString(string value) => new($"FromString:{value}");

		public static BoxWithMismatchedFromString<TValue> Parse(string value) => new($"Parse:{value}");

		public override string ToString() => Raw;
	}

	public interface ITestEncoding;

	public struct HexEncoding : ITestEncoding;

	/// <summary>
	/// Class whose FromString is generic with a constraint the class itself cannot satisfy, so only Parse can produce it.
	/// </summary>
	public sealed class CodeWithConstrainedGenericFromString(string value)
	{
		public string Value { get; } = value;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed", Justification = "Testing a constrained generic overload")]
		public static CodeWithConstrainedGenericFromString FromString<TEncoding>(string value) where TEncoding : struct, ITestEncoding => new($"FromString:{value}");

		public static CodeWithConstrainedGenericFromString Parse(string value) => new($"Parse:{value}");

		public override string ToString() => Value;
	}

	[TestMethod]
	public void FromString_Returning_A_Different_Closed_Generic_Type_Should_Fall_Back_To_Parse()
	{
		JsonSerializerOptions options = GetOptions();

		BoxWithMismatchedFromString<int>? result = JsonSerializer.Deserialize<BoxWithMismatchedFromString<int>>("\"test\"", options);

		Assert.IsNotNull(result);
		Assert.AreEqual("Parse:test", result.Raw, "Parse should be used because FromString returns BoxWithMismatchedFromString<string>");
	}

	[TestMethod]
	public void Generic_FromString_That_Cannot_Be_Closed_Over_The_Type_Should_Fall_Back_To_Parse()
	{
		JsonSerializerOptions options = GetOptions();

		CodeWithConstrainedGenericFromString? result = JsonSerializer.Deserialize<CodeWithConstrainedGenericFromString>("\"test\"", options);

		Assert.IsNotNull(result);
		Assert.AreEqual("Parse:test", result.Value, "Parse should be used because FromString<TEncoding> cannot be closed over the type");
	}
}
