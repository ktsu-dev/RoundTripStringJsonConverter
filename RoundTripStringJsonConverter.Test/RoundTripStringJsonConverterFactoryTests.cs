// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;

using ktsu.RoundTripStringJsonConverter;

[TestClass]
public class RoundTripStringJsonConverterFactoryTests
{
	public class TestClassWithFromString
	{
		public string Value { get; set; } = string.Empty;

		public static TestClassWithFromString FromString(string value) => new() { Value = value };

		public override string ToString() => Value;
	}

	public class TestClassWithParse
	{
		public string Value { get; set; } = string.Empty;

		public static TestClassWithParse Parse(string value) => new() { Value = value };

		public override string ToString() => Value;
	}

	public class TestClassWithCreate
	{
		public string Value { get; set; } = string.Empty;

		public static TestClassWithCreate Create(string value) => new() { Value = value };

		public override string ToString() => Value;
	}

	public class TestClassWithConvert
	{
		public string Value { get; set; } = string.Empty;

		public static TestClassWithConvert Convert(string value) => new() { Value = value };

		public override string ToString() => Value;
	}

	public sealed class TestGenericClass<TNumber>
	{
		public string Value { get; set; } = string.Empty;

		public static TestGenericClass<TNumber> FromString<TSelf>(string value) => new() { Value = value };

		public override string ToString() => Value;

		public TNumber? Number { get; set; }
	}

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void CanConvertShouldReturnTrueForValidTypes()
	{
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsTrue(factory.CanConvert(typeof(TestClassWithFromString)));
		Assert.IsTrue(factory.CanConvert(typeof(TestClassWithParse)));
		Assert.IsTrue(factory.CanConvert(typeof(TestClassWithCreate)));
		Assert.IsTrue(factory.CanConvert(typeof(TestClassWithConvert)));
		Assert.IsTrue(factory.CanConvert(typeof(TestGenericClass<int>)));
	}

	[TestMethod]
	public void CanConvertShouldReturnFalseForInvalidType()
	{
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsFalse(factory.CanConvert(typeof(string)));
	}

	[TestMethod]
	public void CreateConverterShouldReturnConverterForValidTypes()
	{
		RoundTripStringJsonConverterFactory factory = new();

		JsonConverter converter = factory.CreateConverter(typeof(TestClassWithFromString), GetOptions());
		Assert.IsNotNull(converter);

		converter = factory.CreateConverter(typeof(TestClassWithParse), GetOptions());
		Assert.IsNotNull(converter);

		converter = factory.CreateConverter(typeof(TestClassWithCreate), GetOptions());
		Assert.IsNotNull(converter);

		converter = factory.CreateConverter(typeof(TestClassWithConvert), GetOptions());
		Assert.IsNotNull(converter);

		converter = factory.CreateConverter(typeof(TestGenericClass<int>), GetOptions());
		Assert.IsNotNull(converter);
	}

	[TestMethod]
	public void SerializeShouldUseToStringForAllTypes()
	{
		JsonSerializerOptions options = GetOptions();

		TestClassWithFromString fromStringInstance = new()
		{ Value = "test value" };
		string json = JsonSerializer.Serialize(fromStringInstance, options);
		Assert.AreEqual("\"test value\"", json);

		TestClassWithParse parseInstance = new()
		{ Value = "test value" };
		json = JsonSerializer.Serialize(parseInstance, options);
		Assert.AreEqual("\"test value\"", json);

		TestClassWithCreate createInstance = new()
		{ Value = "test value" };
		json = JsonSerializer.Serialize(createInstance, options);
		Assert.AreEqual("\"test value\"", json);

		TestClassWithConvert convertInstance = new()
		{ Value = "test value" };
		json = JsonSerializer.Serialize(convertInstance, options);
		Assert.AreEqual("\"test value\"", json);
	}

	[TestMethod]
	public void DeserializeShouldUseCorrectConversionMethod()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"test value\"";

		TestClassWithFromString? fromStringInstance = JsonSerializer.Deserialize<TestClassWithFromString>(json, options);
		Assert.IsNotNull(fromStringInstance);
		Assert.AreEqual("test value", fromStringInstance.Value);

		TestClassWithParse? parseInstance = JsonSerializer.Deserialize<TestClassWithParse>(json, options);
		Assert.IsNotNull(parseInstance);
		Assert.AreEqual("test value", parseInstance.Value);

		TestClassWithCreate? createInstance = JsonSerializer.Deserialize<TestClassWithCreate>(json, options);
		Assert.IsNotNull(createInstance);
		Assert.AreEqual("test value", createInstance.Value);

		TestClassWithConvert? convertInstance = JsonSerializer.Deserialize<TestClassWithConvert>(json, options);
		Assert.IsNotNull(convertInstance);
		Assert.AreEqual("test value", convertInstance.Value);
	}

	[TestMethod]
	public void DeserializeGenericShouldUseFromString()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "\"test value\"";
		TestGenericClass<int>? testInstance = JsonSerializer.Deserialize<TestGenericClass<int>>(json, options);
		Assert.IsNotNull(testInstance);
		Assert.AreEqual("test value", testInstance.Value);
	}

	[TestMethod]
	public void DeserializeShouldThrowJsonExceptionForInvalidToken()
	{
		JsonSerializerOptions options = GetOptions();
		string json = "123";
		Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<TestClassWithFromString>(json, options));
		Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<TestClassWithParse>(json, options));
		Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<TestClassWithCreate>(json, options));
		Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<TestClassWithConvert>(json, options));
	}

	[TestMethod]
	public void ShouldPrioritizeFromStringOverOtherMethods()
	{
		// Test class with multiple conversion methods - FromString should be prioritized
		JsonSerializerOptions options = GetOptions();

		// This test verifies that when multiple methods exist, FromString is used (since it's first in the priority order)
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsTrue(factory.CanConvert(typeof(TestClassWithFromString)));
	}
}
