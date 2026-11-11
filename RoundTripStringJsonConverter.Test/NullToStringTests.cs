// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A value whose <c>ToString()</c> returns null has no string to write, so writing it fails at once
/// rather than producing JSON the converter can't read back (ktsu-dev/RoundTripStringJsonConverter#138).
/// </summary>
[TestClass]
public class NullToStringTests
{
	public readonly record struct UserId(string Value)
	{
		public static UserId Parse(string value) => new(value);

		public override string ToString() => Value;
	}

	public class Order
	{
		public int Number { get; set; }

		public UserId Owner { get; set; }
	}

	public class Tag
	{
		public string? Value { get; set; }

		public static Tag FromString(string value) => new() { Value = value };

		public override string? ToString() => Value;
	}

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void DefaultStructPropertyShouldThrowAtWrite()
	{
		JsonSerializerOptions options = GetOptions();

		JsonException exception = Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(new Order { Number = 42 }, options));
		Assert.Contains("ToString() returned null", exception.Message);
	}

	[TestMethod]
	public void DefaultStructArrayShouldThrowAtWrite()
	{
		JsonSerializerOptions options = GetOptions();

		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(new UserId[2], options));
	}

	[TestMethod]
	public void DefaultStructDictionaryKeyShouldThrowAtWrite()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<UserId, int> keys = new() { [default] = 1 };

		JsonException exception = Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(keys, options));
		Assert.Contains("ToString() returned null", exception.Message);
	}

	[TestMethod]
	public void ReferenceTypeWithNullToStringShouldThrowAtWrite()
	{
		JsonSerializerOptions options = GetOptions();

		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(new Tag(), options));
		Assert.ThrowsExactly<JsonException>(() => JsonSerializer.Serialize(new Dictionary<Tag, int> { [new Tag()] = 1 }, options));
	}

	[TestMethod]
	public void SetStructStillRoundTrips()
	{
		JsonSerializerOptions options = GetOptions();
		Order order = new() { Number = 42, Owner = UserId.Parse("USER-1") };

		string json = JsonSerializer.Serialize(order, options);
		Order? back = JsonSerializer.Deserialize<Order>(json, options);

		Assert.AreEqual("{\"Number\":42,\"Owner\":\"USER-1\"}", json);
		Assert.IsNotNull(back);
		Assert.AreEqual(order.Owner, back.Owner);
	}

	[TestMethod]
	public void NullReferenceStillWritesNull()
	{
		JsonSerializerOptions options = GetOptions();

		Assert.AreEqual("null", JsonSerializer.Serialize<Tag?>(null, options));
	}
}
