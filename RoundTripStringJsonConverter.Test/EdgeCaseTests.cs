// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EdgeCaseTests
{
	public class ValidStringType(string value)
	{
		public string Value { get; } = value;

		public static ValidStringType FromString(string value) => new(value);

		public override string ToString() => Value;
	}

	public class TypeWithNullHandling(string? value)
	{
		public string? Value { get; } = value;

		public static TypeWithNullHandling FromString(string value) => new(value);

		public override string ToString() => Value ?? string.Empty;
	}

	public class TypeWithExceptionInFromString(string value)
	{
		public string Value { get; } = value;

		public static TypeWithExceptionInFromString FromString(string value)
		{
			return value == "throw" ? throw new ArgumentException("Test exception") : new(value);
		}

		public override string ToString() => Value;
	}

	public class TypeWithExceptionInToString(string value)
	{
		public string Value { get; } = value;

		public static TypeWithExceptionInToString FromString(string value) => new(value);

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations - this is intentional for testing
		public override string ToString()
		{
			return Value == "throw" ? throw new InvalidOperationException("Test exception in ToString") : Value;
		}
#pragma warning restore CA1065
	}

	// Invalid types (should not be convertible)
	public class TypeWithoutConversionMethod
	{
		public string Value { get; set; } = string.Empty;
		public override string ToString() => Value;
	}

	public class TypeWithWrongSignature
	{
		public string Value { get; set; } = string.Empty;

		// Wrong signature - not static (intentionally non-static for testing)
#pragma warning disable CA1822 // Mark members as static - this is intentionally non-static for testing
		public TypeWithWrongSignature FromString(string value) => new() { Value = value };
#pragma warning restore CA1822

		public override string ToString() => Value;
	}

	public class TypeWithAmbiguousFromString(string value)
	{
		public string Value { get; } = value;

		// Two overloads to create ambiguity
		public static TypeWithAmbiguousFromString FromString(string value) => new(value);
		public static TypeWithAmbiguousFromString FromString(string value, IFormatProvider? _) => new(value);

		// Also provide Parse so fallback path in factory can be exercised
		public static TypeWithAmbiguousFromString Parse(string value) => new(value + ":parsed");

		public override string ToString() => Value;
	}

	public class TypeWithWrongParameterType
	{
		public string Value { get; set; } = string.Empty;

		// Wrong parameter type - should be string
		public static TypeWithWrongParameterType FromString(int value) => new() { Value = value.ToString() };

		public override string ToString() => Value;
	}

	public class TypeWithWrongReturnType
	{
		public string Value { get; set; } = string.Empty;

		// Wrong return type - should return the same type
		public static string FromString(string value) => value;

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
	public void Should_Handle_Empty_String()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"\"";

		ValidStringType? result = JsonSerializer.Deserialize<ValidStringType>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual(string.Empty, result.Value);
	}

	[TestMethod]
	public void Should_Handle_Whitespace_String()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"   \"";

		ValidStringType? result = JsonSerializer.Deserialize<ValidStringType>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual("   ", result.Value);
	}

	[TestMethod]
	public void Should_Handle_Special_Characters()
	{
		JsonSerializerOptions options = GetOptions();
		string testValue = "Hello\nWorld\t\"Test\"\\Path";
		string json = JsonSerializer.Serialize(testValue);

		ValidStringType? result = JsonSerializer.Deserialize<ValidStringType>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual(testValue, result.Value);
	}

	[TestMethod]
	public void Should_Handle_Unicode_Characters()
	{
		JsonSerializerOptions options = GetOptions();
		string testValue = "こんにちは 🌟 مرحبا";
		string json = JsonSerializer.Serialize(testValue);

		ValidStringType? result = JsonSerializer.Deserialize<ValidStringType>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual(testValue, result.Value);
	}

	[TestMethod]
	public void Should_Throw_JsonException_For_Non_String_Json()
	{
		JsonSerializerOptions options = GetOptions();

		// Test number
		Assert.ThrowsExactly<JsonException>(() =>
			JsonSerializer.Deserialize<ValidStringType>("123", options));

		// Test boolean
		Assert.ThrowsExactly<JsonException>(() =>
			JsonSerializer.Deserialize<ValidStringType>("true", options));

		// Test object
		Assert.ThrowsExactly<JsonException>(() =>
			JsonSerializer.Deserialize<ValidStringType>("{}", options));

		// Test array
		Assert.ThrowsExactly<JsonException>(() =>
			JsonSerializer.Deserialize<ValidStringType>("[]", options));
	}

	[TestMethod]
	public void Should_Propagate_Exception_From_Conversion_Method()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"throw\"";

		Assert.ThrowsExactly<ArgumentException>(() =>
			JsonSerializer.Deserialize<TypeWithExceptionInFromString>(json, options));
	}

	[TestMethod]
	public void Should_Propagate_Exception_From_ToString()
	{
		JsonSerializerOptions options = GetOptions();
		TypeWithExceptionInToString instance = new("throw");

		Assert.ThrowsExactly<InvalidOperationException>(() =>
			JsonSerializer.Serialize(instance, options));
	}

	[TestMethod]
	public void Should_Not_Convert_Types_Without_Conversion_Method()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(typeof(TypeWithoutConversionMethod)), "Factory should not convert types without a conversion method");
		Assert.IsFalse(factory.CanConvert(typeof(TypeWithWrongSignature)), "Factory should not convert types with non-static FromString method");
		Assert.IsFalse(factory.CanConvert(typeof(TypeWithWrongParameterType)), "Factory should not convert types with wrong parameter type in FromString");
		Assert.IsFalse(factory.CanConvert(typeof(TypeWithWrongReturnType)), "Factory should not convert types with wrong return type in FromString");
	}

	[TestMethod]
	public void Should_Not_Convert_Built_In_Types()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(typeof(string)), "Factory should not convert string type");
		Assert.IsFalse(factory.CanConvert(typeof(int)), "Factory should not convert int type");
		Assert.IsFalse(factory.CanConvert(typeof(DateTime)), "Factory should not convert DateTime type");
		Assert.IsFalse(factory.CanConvert(typeof(Guid)), "Factory should not convert Guid type");
		Assert.IsFalse(factory.CanConvert(typeof(object)), "Factory should not convert object type");
	}

	[TestMethod]
	public void Should_Handle_Ambiguous_FromString_By_Falling_Back()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"abc\"";

		// With ambiguity present, factory's catch(AmbiguousMatchException) path should still resolve a valid method
		TypeWithAmbiguousFromString? result = JsonSerializer.Deserialize<TypeWithAmbiguousFromString>(json, options);
		Assert.IsNotNull(result);
		// Either FromString or Parse may be selected depending on reflection path; both produce non-null; ensure it's one of expected shapes
		Assert.IsTrue(result.Value is "abc" or "abc:parsed", "Deserialized value should match expected FromString or Parse output");
	}

	[TestMethod]
	public void Should_Handle_Round_Trip_With_Long_String()
	{
		JsonSerializerOptions options = GetOptions();
		string longString = new('A', 10000); // 10k characters

		ValidStringType original = ValidStringType.FromString(longString);
		string json = JsonSerializer.Serialize(original, options);
		ValidStringType? deserialized = JsonSerializer.Deserialize<ValidStringType>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(longString, deserialized.Value);
	}

	[TestMethod]
	public void Should_Handle_Null_Argument_Exceptions()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.ThrowsExactly<ArgumentNullException>(() => factory.CanConvert(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => factory.CreateConverter(null!, GetOptions()));
		Assert.ThrowsExactly<ArgumentNullException>(() => factory.CreateConverter(typeof(ValidStringType), null!));
	}
}
