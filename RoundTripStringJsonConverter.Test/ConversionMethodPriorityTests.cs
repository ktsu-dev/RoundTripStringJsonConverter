// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;

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
}
