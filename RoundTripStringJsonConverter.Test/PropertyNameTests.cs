// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

[TestClass]
public class PropertyNameTests
{
	public class CustomKey(string value)
	{
		public string Value { get; } = value;

		public static CustomKey FromString(string value) => new(value);
		public override string ToString() => Value;
		public override bool Equals(object? obj) => obj is CustomKey other && Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();
	}

	public class TestObject
	{
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<CustomKey, string> KeyValuePairs { get; set; } = new Dictionary<CustomKey, string>();
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<CustomKey, CustomKey> KeyKeyPairs { get; set; } = new Dictionary<CustomKey, CustomKey>();
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<CustomKey, List<CustomKey>> KeyListPairs { get; set; } = new Dictionary<CustomKey, List<CustomKey>>();
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
	public void Should_Handle_Complex_Dictionary_Scenarios()
	{
		JsonSerializerOptions options = GetOptions();

		TestObject original = new();

		original.KeyValuePairs.Add(CustomKey.FromString("simple"), "simpleValue");
		original.KeyValuePairs.Add(CustomKey.FromString("complex key"), "complexValue");

		original.KeyKeyPairs.Add(CustomKey.FromString("mapKey1"), CustomKey.FromString("mapValue1"));
		original.KeyKeyPairs.Add(CustomKey.FromString("mapKey2"), CustomKey.FromString("mapValue2"));

		original.KeyListPairs.Add(CustomKey.FromString("listKey1"),
			[CustomKey.FromString("item1"), CustomKey.FromString("item2")]);
		original.KeyListPairs.Add(CustomKey.FromString("listKey2"),
			[CustomKey.FromString("item3")]);

		string json = JsonSerializer.Serialize(original, options);
		TestObject? deserialized = JsonSerializer.Deserialize<TestObject>(json, options);

		Assert.IsNotNull(deserialized);

		// Verify KeyValuePairs
		Assert.AreEqual(2, deserialized.KeyValuePairs.Count);
		Assert.AreEqual("simpleValue", deserialized.KeyValuePairs[CustomKey.FromString("simple")]);
		Assert.AreEqual("complexValue", deserialized.KeyValuePairs[CustomKey.FromString("complex key")]);

		// Verify KeyKeyPairs
		Assert.AreEqual(2, deserialized.KeyKeyPairs.Count);
		Assert.AreEqual("mapValue1", deserialized.KeyKeyPairs[CustomKey.FromString("mapKey1")].Value);
		Assert.AreEqual("mapValue2", deserialized.KeyKeyPairs[CustomKey.FromString("mapKey2")].Value);

		// Verify KeyListPairs
		Assert.AreEqual(2, deserialized.KeyListPairs.Count);
		Assert.AreEqual(2, deserialized.KeyListPairs[CustomKey.FromString("listKey1")].Count);
		Assert.AreEqual(1, deserialized.KeyListPairs[CustomKey.FromString("listKey2")].Count);
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
	public void Should_Handle_Empty_And_Whitespace_Property_Names()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<CustomKey, string> original = new()
		{
			{ CustomKey.FromString(""), "emptyKey" },
			{ CustomKey.FromString(" "), "singleSpace" },
			{ CustomKey.FromString("   "), "multipleSpaces" },
			{ CustomKey.FromString("\t"), "tab" },
			{ CustomKey.FromString("\n"), "newline" }
		};

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<CustomKey, string>? deserialized =
			JsonSerializer.Deserialize<Dictionary<CustomKey, string>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(5, deserialized.Count);

		Assert.AreEqual("emptyKey", deserialized[CustomKey.FromString("")]);
		Assert.AreEqual("singleSpace", deserialized[CustomKey.FromString(" ")]);
		Assert.AreEqual("multipleSpaces", deserialized[CustomKey.FromString("   ")]);
		Assert.AreEqual("tab", deserialized[CustomKey.FromString("\t")]);
		Assert.AreEqual("newline", deserialized[CustomKey.FromString("\n")]);
	}

	[TestMethod]
	public void Should_Handle_Duplicate_String_Representations()
	{
		JsonSerializerOptions options = GetOptions();

		// Note: This test ensures that keys with identical string representations
		// but different object identities are handled correctly
		CustomKey key1 = CustomKey.FromString("duplicate");
		CustomKey key2 = CustomKey.FromString("duplicate");

		Dictionary<CustomKey, string> original = new()
		{
			{ key1, "first" }
		};

		// This should not add a second entry since key1 and key2 are equal
		Assert.IsTrue(key1.Equals(key2));
		Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<CustomKey, string>? deserialized =
			JsonSerializer.Deserialize<Dictionary<CustomKey, string>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(1, deserialized.Count);
		Assert.AreEqual("first", deserialized[key2]); // Should work with key2 due to equality
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
