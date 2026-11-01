// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A type is only claimed when it overrides ToString, the half of the round-trip contract that writes the string.
/// </summary>
[TestClass]
public class ToStringOverrideTests
{
	public sealed class FactoryWidget
	{
		public string Name { get; set; } = string.Empty;

		public int Size { get; set; }

		public static FactoryWidget Create(string name) => new() { Name = name };
	}

	public struct ParseOnlyStruct : System.IEquatable<ParseOnlyStruct>
	{
		public int Value { get; set; }

		public static ParseOnlyStruct Parse(string value) => new() { Value = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture) };

		public readonly bool Equals(ParseOnlyStruct other) => Value == other.Value;

		public override readonly bool Equals(object? obj) => obj is ParseOnlyStruct other && Equals(other);

		public override readonly int GetHashCode() => Value;

		public static bool operator ==(ParseOnlyStruct left, ParseOnlyStruct right) => left.Equals(right);

		public static bool operator !=(ParseOnlyStruct left, ParseOnlyStruct right) => !left.Equals(right);
	}

	public abstract class StringBase(string value)
	{
		public string Value { get; } = value;

		public override string ToString() => Value;
	}

	public sealed class InheritsToString(string value) : StringBase(value)
	{
		public static InheritsToString FromString(string value) => new(value);
	}

	public sealed class WidgetHolder
	{
		public FactoryWidget? W { get; set; }
	}

	private static readonly JsonSerializerOptions Options = new()
	{
		Converters = { new RoundTripStringJsonConverterFactory() },
	};

	[TestMethod]
	public void CanConvert_ClassWithFactoryButNoToStringOverride_ReturnsFalse()
	{
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsFalse(factory.CanConvert(typeof(FactoryWidget)));
	}

	[TestMethod]
	public void CanConvert_StructWithParseButNoToStringOverride_ReturnsFalse()
	{
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsFalse(factory.CanConvert(typeof(ParseOnlyStruct)));
	}

	[TestMethod]
	public void CanConvert_TypeInheritingToStringOverride_ReturnsTrue()
	{
		RoundTripStringJsonConverterFactory factory = new();
		Assert.IsTrue(factory.CanConvert(typeof(InheritsToString)));
	}

	[TestMethod]
	public void RoundTrip_ClassWithFactoryButNoToStringOverride_KeepsItsData()
	{
		FactoryWidget widget = FactoryWidget.Create("gear");
		widget.Size = 3;

		string json = JsonSerializer.Serialize(new WidgetHolder { W = widget }, Options);
		WidgetHolder? back = JsonSerializer.Deserialize<WidgetHolder>(json, Options);

		Assert.AreEqual("{\"W\":{\"Name\":\"gear\",\"Size\":3}}", json);
		Assert.IsNotNull(back?.W);
		Assert.AreEqual("gear", back.W.Name);
		Assert.AreEqual(3, back.W.Size);
	}

	[TestMethod]
	public void RoundTrip_StructWithParseButNoToStringOverride_KeepsItsData()
	{
		string json = JsonSerializer.Serialize(new ParseOnlyStruct { Value = 42 }, Options);
		ParseOnlyStruct back = JsonSerializer.Deserialize<ParseOnlyStruct>(json, Options);

		Assert.AreEqual("{\"Value\":42}", json);
		Assert.AreEqual(42, back.Value);
	}

	[TestMethod]
	public void RoundTrip_TypeInheritingToStringOverride_IsWrittenAsString()
	{
		string json = JsonSerializer.Serialize(InheritsToString.FromString("abc"), Options);
		InheritsToString? back = JsonSerializer.Deserialize<InheritsToString>(json, Options);

		Assert.AreEqual("\"abc\"", json);
		Assert.AreEqual("abc", back?.Value);
	}
}
