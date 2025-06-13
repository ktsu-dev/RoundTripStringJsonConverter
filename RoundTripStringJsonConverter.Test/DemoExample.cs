// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Test;

using System.Text.Json;

/// <summary>
/// Demonstration classes showing different string conversion patterns supported by RoundTripStringJsonConverter.
/// </summary>
public static class DemoExample
{
	// Example class using FromString method
	public class PersonId(string value)
	{
		public string Value { get; } = value;

		public static PersonId FromString(string value) => new(value);

		public override string ToString() => Value;
	}

	// Example class using Parse method (common in .NET)
	public class ProductCode(string code)
	{
		public string Code { get; } = code;

		public static ProductCode Parse(string code) => new(code);

		public override string ToString() => Code;
	}

	// Example class using Create method
	public class OrderId(string id)
	{
		public string Id { get; } = id;

		public static OrderId Create(string id) => new(id);

		public override string ToString() => Id;
	}

	// Example class using Convert method
	public class CategoryName(string name)
	{
		public string Name { get; } = name;

		public static CategoryName Convert(string name) => new(name);

		public override string ToString() => Name;
	}

	// Example showing all types working together
	public class Order
	{
		public OrderId Id { get; set; } = null!;
		public PersonId CustomerId { get; set; } = null!;
		public ProductCode ProductCode { get; set; } = null!;
		public CategoryName Category { get; set; } = null!;
	}

	public static void RunDemo()
	{
		// Set up JSON serializer with our converter
		JsonSerializerOptions options = new()
		{
			WriteIndented = true,
			Converters = { new RoundTripStringJsonConverterFactory() }
		};

		// Create test data
		Order order = new()
		{
			Id = OrderId.Create("ORD-12345"),
			CustomerId = PersonId.FromString("CUST-67890"),
			ProductCode = ProductCode.Parse("PROD-ABC123"),
			Category = CategoryName.Convert("Electronics")
		};

		// Serialize to JSON
		string json = JsonSerializer.Serialize(order, options);
		Console.WriteLine("Serialized Order:");
		Console.WriteLine(json);

		// Deserialize back from JSON
		Order? deserializedOrder = JsonSerializer.Deserialize<Order>(json, options);
		Console.WriteLine("\nDeserialized Order:");
		Console.WriteLine($"Order ID: {deserializedOrder?.Id}");
		Console.WriteLine($"Customer ID: {deserializedOrder?.CustomerId}");
		Console.WriteLine($"Product Code: {deserializedOrder?.ProductCode}");
		Console.WriteLine($"Category: {deserializedOrder?.Category}");

		// Verify round-trip worked correctly
		bool isRoundTripSuccessful =
			order.Id.ToString() == deserializedOrder?.Id?.ToString() &&
			order.CustomerId.ToString() == deserializedOrder?.CustomerId?.ToString() &&
			order.ProductCode.ToString() == deserializedOrder?.ProductCode?.ToString() &&
			order.Category.ToString() == deserializedOrder?.Category?.ToString();

		Console.WriteLine($"\nRound-trip successful: {isRoundTripSuccessful}");
	}
}
