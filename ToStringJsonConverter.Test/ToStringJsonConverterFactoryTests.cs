// Ignore Spelling: Deserialize
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace ktsu.ToStringJsonConverter.Tests;

using System.Text.Json;
using ktsu.ToStringJsonConverter;

[TestClass]
public class ToStringJsonConverterFactoryTests
{
	public class TestClass
	{
		public string Value { get; set; } = string.Empty;

		public static TestClass FromString(string value) => new() { Value = value };

		public override string ToString() => Value;
	}

	public sealed class TestClass<TNumber>
	{
		public string Value { get; set; } = string.Empty;

		public static TestClass<TNumber> FromString<TSelf>(string value) => new() { Value = value };

		public override string ToString() => Value;

		public TNumber? Number { get; set; }
	}

	private static JsonSerializerOptions GetOptions()
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new ToStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void CanConvertShouldReturnTrueForValidType()
	{
		var factory = new ToStringJsonConverterFactory();
		Assert.IsTrue(factory.CanConvert(typeof(TestClass)));
		Assert.IsTrue(factory.CanConvert(typeof(TestClass<int>)));
	}

	[TestMethod]
	public void CanConvertShouldReturnFalseForInvalidType()
	{
		var factory = new ToStringJsonConverterFactory();
		Assert.IsFalse(factory.CanConvert(typeof(string)));
	}

	[TestMethod]
	public void CreateConverterShouldReturnConverterForValidType()
	{
		var factory = new ToStringJsonConverterFactory();
		var converter = factory.CreateConverter(typeof(TestClass), GetOptions());
		Assert.IsNotNull(converter);
		converter = factory.CreateConverter(typeof(TestClass<int>), GetOptions());
		Assert.IsNotNull(converter);
	}

	[TestMethod]
	public void SerializeShouldUseToString()
	{
		var options = GetOptions();
		var testInstance = new TestClass { Value = "test value" };
		string json = JsonSerializer.Serialize(testInstance, options);
		Assert.AreEqual("\"test value\"", json);
	}

	[TestMethod]
	public void SerializeGenericShouldUseToString()
	{
		var options = GetOptions();
		var testInstance = new TestClass<int> { Value = "test value" };
		string json = JsonSerializer.Serialize(testInstance, options);
		Assert.AreEqual("\"test value\"", json);
	}

	[TestMethod]
	public void DeserializeShouldUseFromString()
	{
		var options = GetOptions();
		string json = "\"test value\"";
		var testInstance = JsonSerializer.Deserialize<TestClass>(json, options);
		Assert.IsNotNull(testInstance);
		Assert.AreEqual("test value", testInstance.Value);
	}

	[TestMethod]
	public void DeserializeGenericShouldUseFromString()
	{
		var options = GetOptions();
		string json = "\"test value\"";
		var testInstance = JsonSerializer.Deserialize<TestClass<int>>(json, options);
		Assert.IsNotNull(testInstance);
		Assert.AreEqual("test value", testInstance.Value);
	}

	[TestMethod]
	public void DeserializeShouldThrowJsonExceptionForInvalidToken()
	{
		var options = GetOptions();
		string json = "123";
		Assert.ThrowsException<JsonException>(() => JsonSerializer.Deserialize<TestClass>(json, options));
	}
}
