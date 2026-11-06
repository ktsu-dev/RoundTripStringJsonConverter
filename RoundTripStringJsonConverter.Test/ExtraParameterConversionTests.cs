// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Conversion methods that take parameters after the string, such as the
/// <c>Parse(string, IFormatProvider?)</c> shape of <c>IParsable&lt;T&gt;</c>.
/// </summary>
[TestClass]
public class ExtraParameterConversionTests
{
	/// <summary>
	/// Has only the <c>IParsable&lt;T&gt;</c> parse shape, with no single-string overload.
	/// </summary>
	internal sealed record Temperature(double Celsius)
	{
		public override string ToString() => $"{Celsius.ToString(CultureInfo.InvariantCulture)}C";

		public static Temperature Parse(string s, IFormatProvider? provider) =>
			new(double.Parse(s.TrimEnd('C'), provider));
	}

	/// <summary>
	/// Has a factory whose second parameter is optional.
	/// </summary>
	public sealed record Named(string Name, int Weight)
	{
		public override string ToString() => Name;

		public static Named Create(string name, int weight = 7) => new(name, weight);
	}

	/// <summary>
	/// Has a factory whose second parameter is required, so it cannot be called from a string alone.
	/// </summary>
	public sealed record Weighted(string Name)
	{
		public override string ToString() => Name;

		public static Weighted Create(string name, int weight) => new($"{name}:{weight.ToString(CultureInfo.InvariantCulture)}");
	}

	/// <summary>
	/// Has both a single-string and an <see cref="IFormatProvider"/> overload.
	/// </summary>
	public sealed record Overloaded(string Value)
	{
		public override string ToString() => Value;

		public static Overloaded Parse(string s, IFormatProvider? provider) => new($"provider:{s}:{provider is not null}");

		public static Overloaded Parse(string s) => new(s);
	}

	/// <summary>
	/// Has a parse method whose value-type second parameter is <c>[Optional]</c> with no declared default.
	/// </summary>
	public sealed record Radixed(string Digits, int Radix)
	{
		public override string ToString() => Digits;

		public static Radixed Parse(string s, [Optional] int radix) => new(s, radix);
	}

	/// <summary>
	/// Has a parse method whose reference-type second parameter is <c>[Optional]</c> with no declared default.
	/// </summary>
	public sealed record Suffixed(string Value, string? Suffix)
	{
		public override string ToString() => Value;

		public static Suffixed Parse(string s, [Optional] string? suffix) => new(s, suffix);
	}

	/// <summary>
	/// Has a parse method whose nullable second parameter is <c>[Optional]</c> with no declared default.
	/// </summary>
	public sealed record MaybeScaled(string Value, int? Scale)
	{
		public override string ToString() => Value;

		public static MaybeScaled Parse(string s, [Optional] int? scale) => new(s, scale);
	}

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	public void ParseWithFormatProviderShouldRoundTrip()
	{
		JsonSerializerOptions options = GetOptions();
		Temperature original = new(21.5);

		string json = JsonSerializer.Serialize(original, options);
		Temperature? result = JsonSerializer.Deserialize<Temperature>(json, options);

		Assert.AreEqual("\"21.5C\"", json);
		Assert.AreEqual(original, result);
	}

	[TestMethod]
	public void CreateWithOptionalParameterShouldRoundTripUsingDefault()
	{
		JsonSerializerOptions options = GetOptions();

		Named? result = JsonSerializer.Deserialize<Named>("\"x\"", options);

		Assert.IsNotNull(result);
		Assert.AreEqual("x", result.Name);
		Assert.AreEqual(7, result.Weight);
	}

	[TestMethod]
	public void ParseWithFormatProviderShouldWorkAsDictionaryKey()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<Temperature, string> original = new() { [new(10)] = "cool", [new(30)] = "warm" };

		string json = JsonSerializer.Serialize(original, options);
		Dictionary<Temperature, string>? result = JsonSerializer.Deserialize<Dictionary<Temperature, string>>(json, options);

		Assert.IsNotNull(result);
		Assert.AreEqual("cool", result[new(10)]);
		Assert.AreEqual("warm", result[new(30)]);
	}

	[TestMethod]
	public void CanConvertShouldRejectFactoryWithRequiredExtraParameter()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(typeof(Weighted)));
	}

	[TestMethod]
	public void SingleStringOverloadShouldBePreferred()
	{
		JsonSerializerOptions options = GetOptions();

		Overloaded? result = JsonSerializer.Deserialize<Overloaded>("\"value\"", options);

		Assert.IsNotNull(result);
		Assert.AreEqual("value", result.Value);
	}

	[TestMethod]
	public void OptionalValueTypeParameterWithoutDefaultShouldRoundTripAsZero()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new Radixed("ff", 16), options);
		Radixed? result = JsonSerializer.Deserialize<Radixed>(json, options);

		Assert.AreEqual("\"ff\"", json);
		Assert.AreEqual(new Radixed("ff", 0), result);
	}

	[TestMethod]
	public void OptionalReferenceTypeParameterWithoutDefaultShouldRoundTripAsNull()
	{
		JsonSerializerOptions options = GetOptions();

		Suffixed? result = JsonSerializer.Deserialize<Suffixed>(JsonSerializer.Serialize(new Suffixed("x", "!"), options), options);

		Assert.AreEqual(new Suffixed("x", null), result);
	}

	[TestMethod]
	public void OptionalNullableParameterWithoutDefaultShouldRoundTripAsNull()
	{
		JsonSerializerOptions options = GetOptions();

		MaybeScaled? result = JsonSerializer.Deserialize<MaybeScaled>(JsonSerializer.Serialize(new MaybeScaled("x", 3), options), options);

		Assert.AreEqual(new MaybeScaled("x", null), result);
	}

	[TestMethod]
	public void OptionalParameterWithoutDefaultShouldWorkAsDictionaryKey()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<Radixed, int> original = new() { [new Radixed("ff", 0)] = 1 };

		Dictionary<Radixed, int>? result = JsonSerializer.Deserialize<Dictionary<Radixed, int>>(JsonSerializer.Serialize(original, options), options);

		Assert.IsNotNull(result);
		Assert.AreEqual(1, result[new Radixed("ff", 0)]);
	}
}
