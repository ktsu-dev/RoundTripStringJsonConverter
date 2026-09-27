// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Globalization;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class NullTokenTests
{
	public readonly record struct Id(int Value)
	{
		public static Id Parse(string s) => new(int.Parse(s, CultureInfo.InvariantCulture));

		public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
	}

	public class IdHolder
	{
		public Id Id { get; set; }
	}

	public class NullableIdHolder
	{
		public Id? Id { get; set; }
	}

	public class Name(string value)
	{
		public string Value { get; } = value;

		public static Name FromString(string value) => new(value);

		public override string ToString() => Value;
	}

	public class NameHolder
	{
		public Name? Name { get; set; }
	}

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void NullForNonNullableStructPropertyShouldThrow()
	{
		JsonSerializerOptions options = GetOptions();

		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<IdHolder>("{\"Id\":null}", options));
	}

	[TestMethod]
	public void NullForTopLevelNonNullableStructShouldThrow()
	{
		JsonSerializerOptions options = GetOptions();

		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Deserialize<Id>("null", options));
	}

	[TestMethod]
	public void NullForNullableStructPropertyShouldReadAsNull()
	{
		JsonSerializerOptions options = GetOptions();

		NullableIdHolder? result = JsonSerializer.Deserialize<NullableIdHolder>("{\"Id\":null}", options);

		Assert.IsNotNull(result);
		Assert.IsNull(result.Id);
	}

	[TestMethod]
	public void NullableStructPropertyShouldRoundTripAValue()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new NullableIdHolder { Id = new Id(42) }, options);
		NullableIdHolder? result = JsonSerializer.Deserialize<NullableIdHolder>(json, options);

		Assert.AreEqual("{\"Id\":\"42\"}", json);
		Assert.IsNotNull(result);
		Assert.AreEqual(new Id(42), result.Id);
	}

	[TestMethod]
	public void NullForReferenceTypePropertyShouldReadAsNull()
	{
		JsonSerializerOptions options = GetOptions();

		NameHolder? result = JsonSerializer.Deserialize<NameHolder>("{\"Name\":null}", options);

		Assert.IsNotNull(result);
		Assert.IsNull(result.Name);
	}
}
