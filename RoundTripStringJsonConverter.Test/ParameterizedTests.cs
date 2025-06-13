// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;

[TestClass]
public class ParameterizedTests
{
	public class TestType
	{
		public string Value { get; }
		public string Method { get; }

		public TestType(string value, string method) => (Value, Method) = (value, method);

		public static TestType FromString(string value) => new(value, "FromString");
		public static TestType Parse(string value) => new(value, "Parse");
		public static TestType Create(string value) => new(value, "Create");
		public static TestType Convert(string value) => new(value, "Convert");

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
	[DataRow("")]
	[DataRow("simple")]
	[DataRow("with spaces")]
	[DataRow("with\nnewlines")]
	[DataRow("with\ttabs")]
	[DataRow("with\"quotes")]
	[DataRow("with\\backslashes")]
	[DataRow("with/forward/slashes")]
	[DataRow("unicode: こんにちは")]
	[DataRow("emoji: 🌟🎉")]
	[DataRow("numbers: 123456")]
	[DataRow("special: !@#$%^&*()")]
	public void Should_Handle_Various_String_Values(string testValue)
	{
		JsonSerializerOptions options = GetOptions();

		TestType original = TestType.FromString(testValue);
		string json = JsonSerializer.Serialize(original, options);
		TestType? deserialized = JsonSerializer.Deserialize<TestType>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(testValue, deserialized.Value);
		Assert.AreEqual("FromString", deserialized.Method);
	}

	[TestMethod]
	[DataRow("123")]
	[DataRow("true")]
	[DataRow("false")]
	[DataRow("null")]
	[DataRow("{}")]
	[DataRow("[]")]
	[DataRow("[1,2,3]")]
	[DataRow(/*lang=json,strict*/ "{\"key\":\"value\"}")]
	public void Should_Throw_JsonException_For_Non_String_Json(string invalidJson)
	{
		JsonSerializerOptions options = GetOptions();

		Assert.ThrowsException<JsonException>(() =>
			JsonSerializer.Deserialize<TestType>(invalidJson, options));
	}

	public static IEnumerable<object[]> BuiltInTypes
	{
		get
		{
			yield return new object[] { typeof(string) };
			yield return new object[] { typeof(int) };
			yield return new object[] { typeof(long) };
			yield return new object[] { typeof(double) };
			yield return new object[] { typeof(decimal) };
			yield return new object[] { typeof(bool) };
			yield return new object[] { typeof(DateTime) };
			yield return new object[] { typeof(DateTimeOffset) };
			yield return new object[] { typeof(Guid) };
			yield return new object[] { typeof(TimeSpan) };
			yield return new object[] { typeof(object) };
			yield return new object[] { typeof(Array) };
			yield return new object[] { typeof(List<string>) };
			yield return new object[] { typeof(Dictionary<string, object>) };
		}
	}

	[TestMethod]
	[DynamicData(nameof(BuiltInTypes), DynamicDataSourceType.Property)]
	public void Should_Not_Convert_Built_In_Types(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsFalse(factory.CanConvert(type), $"Should not convert built-in type: {type.Name}");
	}

	public static IEnumerable<object[]> StringLengths
	{
		get
		{
			yield return new object[] { 0 };      // Empty
			yield return new object[] { 1 };      // Single char
			yield return new object[] { 10 };     // Short
			yield return new object[] { 100 };    // Medium
			yield return new object[] { 1000 };   // Long
			yield return new object[] { 10000 };  // Very long
		}
	}

	[TestMethod]
	[DynamicData(nameof(StringLengths), DynamicDataSourceType.Property)]
	public void Should_Handle_Various_String_Lengths(int length)
	{
		JsonSerializerOptions options = GetOptions();
		string testValue = new('A', length);

		TestType original = TestType.FromString(testValue);
		string json = JsonSerializer.Serialize(original, options);
		TestType? deserialized = JsonSerializer.Deserialize<TestType>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(testValue, deserialized.Value);
		Assert.AreEqual(length, deserialized.Value.Length);
	}

	public static IEnumerable<object[]> RepeatedValues
	{
		get
		{
			string[] values = ["test1", "test2", "test1", "test3", "test1"];
			for (int i = 0; i < values.Length; i++)
			{
				yield return new object[] { values[i], i };
			}
		}
	}

	[TestMethod]
	[DynamicData(nameof(RepeatedValues), DynamicDataSourceType.Property)]
	public void Should_Handle_Repeated_Serialization_Consistently(string value, int iteration)
	{
		JsonSerializerOptions options = GetOptions();

		TestType original = TestType.FromString(value);
		string json = JsonSerializer.Serialize(original, options);
		TestType? deserialized = JsonSerializer.Deserialize<TestType>(json, options);

		Assert.IsNotNull(deserialized, $"Iteration {iteration}: Deserialized object should not be null");
		Assert.AreEqual(value, deserialized.Value, $"Iteration {iteration}: Value should match");
		Assert.AreEqual("FromString", deserialized.Method, $"Iteration {iteration}: Method should be FromString");
	}
}
