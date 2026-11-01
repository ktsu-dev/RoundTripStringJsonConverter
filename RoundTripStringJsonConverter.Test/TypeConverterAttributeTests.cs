// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A type that names its own converter with a type-level [JsonConverter] keeps it when the factory is
/// registered in the options, even though the factory would otherwise claim it through its Parse method.
/// </summary>
[TestClass]
public class TypeConverterAttributeTests
{
	[JsonConverter(typeof(MoneyJsonConverter))]
	public readonly record struct Money(decimal Amount, string Currency)
	{
		public static Money Parse(string s)
		{
			ArgumentNullException.ThrowIfNull(s);
			string[] parts = s.Split(' ');
			return new Money(decimal.Parse(parts[0], CultureInfo.InvariantCulture), parts[1]);
		}

		public override string ToString() => $"{Amount.ToString(CultureInfo.InvariantCulture)} {Currency}";
	}

	public sealed class MoneyJsonConverter : JsonConverter<Money>
	{
		public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			decimal amount = 0;
			string currency = string.Empty;
			while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
			{
				string name = reader.GetString()!;
				reader.Read();
				if (name == "amount")
				{
					amount = reader.GetDecimal();
				}
				else
				{
					currency = reader.GetString()!;
				}
			}

			return new Money(amount, currency);
		}

		public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(writer);
			writer.WriteStartObject();
			writer.WriteNumber("amount", value.Amount);
			writer.WriteString("currency", value.Currency);
			writer.WriteEndObject();
		}
	}

	public record class Unattributed(string Value)
	{
		public static Unattributed Parse(string s) => new(s);

		public override string ToString() => Value;
	}

	public class Order
	{
		public Money Total { get; set; }
		public Unattributed? Note { get; set; }
	}

	private static JsonSerializerOptions GetOptions() => new() { Converters = { new RoundTripStringJsonConverterFactory() } };

	[TestMethod]
	public void CanConvertShouldRejectTypeWithOwnJsonConverter()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(typeof(Money)));
		Assert.IsFalse(factory.CanConvert(typeof(Money?)));
	}

	[TestMethod]
	public void TypeWithOwnJsonConverterShouldSerializeThroughIt()
	{
		Order order = new() { Total = new Money(1.50m, "USD"), Note = new Unattributed("rush") };

		string json = JsonSerializer.Serialize(order, GetOptions());

		Assert.AreEqual("{\"Total\":{\"amount\":1.50,\"currency\":\"USD\"},\"Note\":\"rush\"}", json);
	}

	[TestMethod]
	public void TypeWithOwnJsonConverterShouldDeserializeThroughIt()
	{
		Order? order = JsonSerializer.Deserialize<Order>("{\"Total\":{\"amount\":1.50,\"currency\":\"USD\"},\"Note\":\"rush\"}", GetOptions());

		Assert.IsNotNull(order);
		Assert.AreEqual(new Money(1.50m, "USD"), order.Total);
		Assert.AreEqual(new Unattributed("rush"), order.Note);
	}

	[TestMethod]
	public void UnattributedTypeShouldStillBeConverted()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsTrue(factory.CanConvert(typeof(Unattributed)));
	}
}
