// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Enums are left to System.Text.Json, so registering the factory doesn't change their wire format.
/// System.Enum's public static Parse&lt;TEnum&gt;(string) must not be taken as a conversion method.
/// </summary>
[TestClass]
public class EnumTests
{
	public enum Color
	{
		Red,
		Green,
	}

	[Flags]
	public enum Permissions
	{
		None = 0,
		Read = 1,
		Write = 2,
	}

	public class Box
	{
		public Color C { get; set; }
		public Permissions P { get; set; }
		public Color? N { get; set; }
	}

	private static JsonSerializerOptions GetOptions(params JsonConverter[] converters)
	{
		JsonSerializerOptions options = new();
		foreach (JsonConverter converter in converters)
		{
			options.Converters.Add(converter);
		}

		return options;
	}

	[TestMethod]
	[DataRow(typeof(Color))]
	[DataRow(typeof(Permissions))]
	[DataRow(typeof(Color?))]
	public void CanConvertShouldRejectEnums(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(type));
	}

	[TestMethod]
	public void EnumsShouldSerializeAsNumbers()
	{
		JsonSerializerOptions options = GetOptions(new RoundTripStringJsonConverterFactory());
		Box box = new() { C = Color.Green, P = Permissions.Read | Permissions.Write, N = Color.Green };

		string json = JsonSerializer.Serialize(box, options);

		Assert.AreEqual("{\"C\":1,\"P\":3,\"N\":1}", json);
	}

	[TestMethod]
	public void NumericEnumJsonShouldDeserialize()
	{
		JsonSerializerOptions options = GetOptions(new RoundTripStringJsonConverterFactory());

		Box? box = JsonSerializer.Deserialize<Box>("{\"C\":1,\"P\":3,\"N\":1}", options);

		Assert.IsNotNull(box);
		Assert.AreEqual(Color.Green, box.C);
		Assert.AreEqual(Permissions.Read | Permissions.Write, box.P);
		Assert.AreEqual(Color.Green, box.N);
	}

	[TestMethod]
	public void StringEnumConverterNamingPolicyShouldApplyWhenRegisteredAfterFactory()
	{
		JsonSerializerOptions options = GetOptions(
			new RoundTripStringJsonConverterFactory(),
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
		Box box = new() { C = Color.Green, P = Permissions.Read, N = Color.Red };

		string json = JsonSerializer.Serialize(box, options);

		Assert.AreEqual("{\"C\":\"green\",\"P\":\"read\",\"N\":\"red\"}", json);
		Assert.AreEqual(Color.Green, JsonSerializer.Deserialize<Box>(json, options)!.C);
	}

	[TestMethod]
	public void StringEnumConverterNamingPolicyShouldApplyWhenRegisteredBeforeFactory()
	{
		JsonSerializerOptions options = GetOptions(
			new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
			new RoundTripStringJsonConverterFactory());
		Box box = new() { C = Color.Green, P = Permissions.Read, N = Color.Red };

		string json = JsonSerializer.Serialize(box, options);

		Assert.AreEqual("{\"C\":\"green\",\"P\":\"read\",\"N\":\"red\"}", json);
	}
}
