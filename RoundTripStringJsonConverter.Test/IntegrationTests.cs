// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;

[TestClass]
public class IntegrationTests
{
	public class UserId(string value)
	{
		public string Value { get; } = value;

		public static UserId FromString(string value) => new(value);
		public override string ToString() => Value;
		public override bool Equals(object? obj) => obj is UserId other && Value == other.Value;
		public override int GetHashCode() => Value.GetHashCode();
	}

	public class ProductCode(string code)
	{
		public string Code { get; } = code;

		public static ProductCode Parse(string code) => new(code);
		public override string ToString() => Code;
		public override bool Equals(object? obj) => obj is ProductCode other && Code == other.Code;
		public override int GetHashCode() => Code.GetHashCode();
	}

	public class OrderId(string id)
	{
		public string Id { get; } = id;

		public static OrderId Create(string id) => new(id);
		public override string ToString() => Id;
	}

	public class CategoryName(string name)
	{
		public string Name { get; } = name;

		public static CategoryName Convert(string name) => new(name);
		public override string ToString() => Name;
	}

	public class Order
	{
		public OrderId Id { get; set; } = null!;
		public UserId CustomerId { get; set; } = null!;
		public List<ProductCode> Products { get; set; } = [];
		public Dictionary<CategoryName, int> CategoryCounts { get; set; } = [];
		public DateTime OrderDate { get; set; }
	}

	public class OrderSummary
	{
		public List<Order> Orders { get; set; } = [];
		public Dictionary<UserId, int> CustomerOrderCounts { get; set; } = [];
		public ProductCode? MostPopularProduct { get; set; }
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
	public void Should_Handle_Complex_Object_With_Multiple_Custom_Types()
	{
		JsonSerializerOptions options = GetOptions();

		Order original = new()
		{
			Id = OrderId.Create("ORD-001"),
			CustomerId = UserId.FromString("USER-123"),
			Products = [
				ProductCode.Parse("PROD-A"),
				ProductCode.Parse("PROD-B"),
				ProductCode.Parse("PROD-C")
			],
			CategoryCounts = new Dictionary<CategoryName, int>
			{
				{ CategoryName.Convert("Electronics"), 2 },
				{ CategoryName.Convert("Books"), 1 }
			},
			OrderDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
		};

		string json = JsonSerializer.Serialize(original, options);
		Order? deserialized = JsonSerializer.Deserialize<Order>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("ORD-001", deserialized.Id.Id);
		Assert.AreEqual("USER-123", deserialized.CustomerId.Value);
		Assert.AreEqual(3, deserialized.Products.Count);
		Assert.AreEqual("PROD-A", deserialized.Products[0].Code);
		Assert.AreEqual("PROD-B", deserialized.Products[1].Code);
		Assert.AreEqual("PROD-C", deserialized.Products[2].Code);
		Assert.AreEqual(2, deserialized.CategoryCounts.Count);
		Assert.AreEqual(original.OrderDate, deserialized.OrderDate);
	}

	[TestMethod]
	public void Should_Handle_Collections_As_Dictionary_Keys()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<UserId, List<ProductCode>> userProducts = new()
		{
			{
				UserId.FromString("USER-001"),
				[ProductCode.Parse("PROD-X"), ProductCode.Parse("PROD-Y")]
			},
			{
				UserId.FromString("USER-002"),
				[ProductCode.Parse("PROD-Z")]
			}
		};

		string json = JsonSerializer.Serialize(userProducts, options);
		Dictionary<UserId, List<ProductCode>>? deserialized =
			JsonSerializer.Deserialize<Dictionary<UserId, List<ProductCode>>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(2, deserialized.Count);

		UserId user1 = UserId.FromString("USER-001");
		UserId user2 = UserId.FromString("USER-002");

		Assert.IsTrue(deserialized.ContainsKey(user1));
		Assert.IsTrue(deserialized.ContainsKey(user2));
		Assert.AreEqual(2, deserialized[user1].Count);
		Assert.AreEqual(1, deserialized[user2].Count);
	}

	[TestMethod]
	public void Should_Handle_Nested_Complex_Structure()
	{
		JsonSerializerOptions options = GetOptions();

		OrderSummary original = new()
		{
			Orders = [
				new Order
				{
					Id = OrderId.Create("ORD-001"),
					CustomerId = UserId.FromString("USER-A"),
					Products = [ProductCode.Parse("PROD-1"), ProductCode.Parse("PROD-2")],
					CategoryCounts = new Dictionary<CategoryName, int>
					{
						{ CategoryName.Convert("Tech"), 1 }
					},
					OrderDate = DateTime.UtcNow
				},
				new Order
				{
					Id = OrderId.Create("ORD-002"),
					CustomerId = UserId.FromString("USER-B"),
					Products = [ProductCode.Parse("PROD-1")],
					CategoryCounts = new Dictionary<CategoryName, int>
					{
						{ CategoryName.Convert("Books"), 1 }
					},
					OrderDate = DateTime.UtcNow.AddDays(-1)
				}
			],
			CustomerOrderCounts = new Dictionary<UserId, int>
			{
				{ UserId.FromString("USER-A"), 1 },
				{ UserId.FromString("USER-B"), 1 }
			},
			MostPopularProduct = ProductCode.Parse("PROD-1")
		};

		string json = JsonSerializer.Serialize(original, options);
		OrderSummary? deserialized = JsonSerializer.Deserialize<OrderSummary>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(2, deserialized.Orders.Count);
		Assert.AreEqual(2, deserialized.CustomerOrderCounts.Count);
		Assert.IsNotNull(deserialized.MostPopularProduct);
		Assert.AreEqual("PROD-1", deserialized.MostPopularProduct.Code);

		// Verify first order
		Order firstOrder = deserialized.Orders[0];
		Assert.AreEqual("ORD-001", firstOrder.Id.Id);
		Assert.AreEqual("USER-A", firstOrder.CustomerId.Value);
		Assert.AreEqual(2, firstOrder.Products.Count);
	}

	[TestMethod]
	public void Should_Handle_Large_Collection_Performance()
	{
		JsonSerializerOptions options = GetOptions();

		// Create a large collection
		List<UserId> largeList = [];
		for (int i = 0; i < 1000; i++)
		{
			largeList.Add(UserId.FromString($"USER-{i:D4}"));
		}

		Dictionary<ProductCode, List<UserId>> productUsers = [];
		for (int i = 0; i < 10; i++)
		{
			productUsers[ProductCode.Parse($"PROD-{i}")] = [.. largeList.Take(100)];
		}

		string json = JsonSerializer.Serialize(productUsers, options);
		Dictionary<ProductCode, List<UserId>>? deserialized =
			JsonSerializer.Deserialize<Dictionary<ProductCode, List<UserId>>>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(10, deserialized.Count);

		ProductCode firstProduct = ProductCode.Parse("PROD-0");
		Assert.IsTrue(deserialized.ContainsKey(firstProduct));
		Assert.AreEqual(100, deserialized[firstProduct].Count);
		Assert.AreEqual("USER-0000", deserialized[firstProduct][0].Value);
	}

	[TestMethod]
	public void Should_Handle_Mixed_Collection_Types()
	{
		JsonSerializerOptions options = GetOptions();

		object[] mixedArray = [
			UserId.FromString("USER-001"),
			"regular string",
			42,
			ProductCode.Parse("PROD-ABC"),
			true,
			OrderId.Create("ORD-999")
		];

		string json = JsonSerializer.Serialize(mixedArray, options);
		object[]? deserialized = JsonSerializer.Deserialize<object[]>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(6, deserialized.Length);

		// Note: Due to JSON serialization, custom types will be serialized as strings
		// and may not deserialize back to the original types in mixed collections
		// This test verifies the serialization doesn't fail
	}

	[TestMethod]
	public void Should_Maintain_Performance_With_Repeated_Conversions()
	{
		JsonSerializerOptions options = GetOptions();
		UserId userId = UserId.FromString("PERF-TEST-USER");

		// Perform multiple serialization/deserialization cycles
		for (int i = 0; i < 100; i++)
		{
			string json = JsonSerializer.Serialize(userId, options);
			UserId? result = JsonSerializer.Deserialize<UserId>(json, options);

			Assert.IsNotNull(result);
			Assert.AreEqual("PERF-TEST-USER", result.Value);
		}
	}
}
