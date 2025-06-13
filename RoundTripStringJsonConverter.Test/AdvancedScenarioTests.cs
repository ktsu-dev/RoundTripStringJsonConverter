// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;

[TestClass]
public class AdvancedScenarioTests
{
	public class CustomKey(string value)
	{
		public string Value { get; } = value;

		public static CustomKey FromString(string value) => new(value);
		public override string ToString() => Value;
		public override bool Equals(object? obj) => obj is CustomKey other && Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();
	}

	private static JsonSerializerOptions GetOptions()
	{
		return new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters = { new RoundTripStringJsonConverterFactory() }
		};
	}

	[TestMethod]
	public void Should_Handle_Custom_Types_As_Dictionary_Keys()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<CustomKey, string> original = new()
		{
			{ CustomKey.FromString("key1"), "value1" },
			{ CustomKey.FromString("key2"), "value2" },
			{ CustomKey.FromString("key with spaces"), "value3" },
			{ CustomKey.FromString("key-with-dashes"), "value4" }
		};

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<CustomKey, string>? deserialized =
			JsonSerializer.Deserialize<Dictionary<CustomKey, string>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(4, deserialized.Count);

		// Verify all keys and values
		Assert.IsTrue(deserialized.ContainsKey(CustomKey.FromString("key1")));
		Assert.IsTrue(deserialized.ContainsKey(CustomKey.FromString("key2")));
		Assert.IsTrue(deserialized.ContainsKey(CustomKey.FromString("key with spaces")));
		Assert.IsTrue(deserialized.ContainsKey(CustomKey.FromString("key-with-dashes")));

		Assert.AreEqual("value1", deserialized[CustomKey.FromString("key1")]);
		Assert.AreEqual("value2", deserialized[CustomKey.FromString("key2")]);
		Assert.AreEqual("value3", deserialized[CustomKey.FromString("key with spaces")]);
		Assert.AreEqual("value4", deserialized[CustomKey.FromString("key-with-dashes")]);
	}

	[TestMethod]
	public void Should_Handle_Special_Characters_In_Property_Names()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<CustomKey, string> original = new()
		{
			{ CustomKey.FromString("key with spaces"), "value1" },
			{ CustomKey.FromString("key.with.dots"), "value2" },
			{ CustomKey.FromString("key/with/slashes"), "value3" },
			{ CustomKey.FromString("key\\with\\backslashes"), "value4" },
			{ CustomKey.FromString("key\"with\"quotes"), "value5" },
			{ CustomKey.FromString("key\nwith\nnewlines"), "value6" },
			{ CustomKey.FromString("key\twith\ttabs"), "value7" },
			{ CustomKey.FromString("🔑 emoji key 🔑"), "value8" }
		};

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<CustomKey, string>? deserialized =
			JsonSerializer.Deserialize<Dictionary<CustomKey, string>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(8, deserialized.Count);

		// Verify all special character keys work
		foreach (KeyValuePair<CustomKey, string> kvp in original)
		{
			Assert.IsTrue(deserialized.ContainsKey(kvp.Key),
				$"Should contain key: {kvp.Key.Value}");
			Assert.AreEqual(kvp.Value, deserialized[kvp.Key],
				$"Value should match for key: {kvp.Key.Value}");
		}
	}

	[TestMethod]
	public void Should_Maintain_Case_Sensitivity_In_Property_Names()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<CustomKey, string> original = new()
		{
			{ CustomKey.FromString("lowercase"), "value1" },
			{ CustomKey.FromString("UPPERCASE"), "value2" },
			{ CustomKey.FromString("MixedCase"), "value3" },
			{ CustomKey.FromString("camelCase"), "value4" },
			{ CustomKey.FromString("PascalCase"), "value5" }
		};

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<CustomKey, string>? deserialized =
			JsonSerializer.Deserialize<Dictionary<CustomKey, string>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(5, deserialized.Count);

		// Verify case sensitivity is maintained
		Assert.AreEqual("value1", deserialized[CustomKey.FromString("lowercase")]);
		Assert.AreEqual("value2", deserialized[CustomKey.FromString("UPPERCASE")]);
		Assert.AreEqual("value3", deserialized[CustomKey.FromString("MixedCase")]);
		Assert.AreEqual("value4", deserialized[CustomKey.FromString("camelCase")]);
		Assert.AreEqual("value5", deserialized[CustomKey.FromString("PascalCase")]);

		// Verify that different cases are treated as different keys
		Assert.IsFalse(deserialized.ContainsKey(CustomKey.FromString("LOWERCASE")));
		Assert.IsFalse(deserialized.ContainsKey(CustomKey.FromString("uppercase")));
	}
}
