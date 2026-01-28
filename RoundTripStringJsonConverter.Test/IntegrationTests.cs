// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IList<ProductCode> Products { get; set; } = [];
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<CategoryName, int> CategoryCounts { get; set; } = new Dictionary<CategoryName, int>();
		public DateTime OrderDate { get; set; }
	}

	public class OrderSummary
	{
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IList<Order> Orders { get; set; } = [];
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<UserId, int> CustomerOrderCounts { get; set; } = new Dictionary<UserId, int>();
		public ProductCode? MostPopularProduct { get; set; }
	}

	private static JsonSerializerOptions GetOptions()
	{
		return new JsonSerializerOptions
		{
			WriteIndented = true,
			IncludeFields = true,
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
			OrderDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
		};

		original.Products.Add(ProductCode.Parse("PROD-A"));
		original.Products.Add(ProductCode.Parse("PROD-B"));
		original.Products.Add(ProductCode.Parse("PROD-C"));

		original.CategoryCounts.Add(CategoryName.Convert("Electronics"), 2);
		original.CategoryCounts.Add(CategoryName.Convert("Books"), 1);

		string json = JsonSerializer.Serialize(original, options);
		Order? deserialized = JsonSerializer.Deserialize<Order>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("ORD-001", deserialized.Id.Id);
		Assert.AreEqual("USER-123", deserialized.CustomerId.Value);
		Assert.HasCount(3, deserialized.Products);
		Assert.AreEqual("PROD-A", deserialized.Products[0].Code);
		Assert.AreEqual("PROD-B", deserialized.Products[1].Code);
		Assert.AreEqual("PROD-C", deserialized.Products[2].Code);
		Assert.HasCount(2, deserialized.CategoryCounts);
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
		Assert.HasCount(2, deserialized);

		UserId user1 = UserId.FromString("USER-001");
		UserId user2 = UserId.FromString("USER-002");

		Assert.IsTrue(deserialized.ContainsKey(user1));
		Assert.IsTrue(deserialized.ContainsKey(user2));
		Assert.HasCount(2, deserialized[user1]);
		Assert.HasCount(1, deserialized[user2]);
	}

	[TestMethod]
	public void Should_Handle_Nested_Complex_Structure()
	{
		JsonSerializerOptions options = GetOptions();

		OrderSummary original = new()
		{
			MostPopularProduct = ProductCode.Parse("PROD-1")
		};

		Order order1 = new()
		{
			Id = OrderId.Create("ORD-001"),
			CustomerId = UserId.FromString("USER-A"),
			OrderDate = DateTime.UtcNow
		};
		order1.Products.Add(ProductCode.Parse("PROD-1"));
		order1.Products.Add(ProductCode.Parse("PROD-2"));
		order1.CategoryCounts.Add(CategoryName.Convert("Tech"), 1);

		Order order2 = new()
		{
			Id = OrderId.Create("ORD-002"),
			CustomerId = UserId.FromString("USER-B"),
			OrderDate = DateTime.UtcNow.AddDays(-1)
		};
		order2.Products.Add(ProductCode.Parse("PROD-1"));
		order2.CategoryCounts.Add(CategoryName.Convert("Books"), 1);

		original.Orders.Add(order1);
		original.Orders.Add(order2);
		original.CustomerOrderCounts.Add(UserId.FromString("USER-A"), 1);
		original.CustomerOrderCounts.Add(UserId.FromString("USER-B"), 1);

		string json = JsonSerializer.Serialize(original, options);
		OrderSummary? deserialized = JsonSerializer.Deserialize<OrderSummary>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.HasCount(2, deserialized.Orders);
		Assert.HasCount(2, deserialized.CustomerOrderCounts);
		Assert.IsNotNull(deserialized.MostPopularProduct);
		Assert.AreEqual("PROD-1", deserialized.MostPopularProduct.Code);

		// Verify first order
		Order firstOrder = deserialized.Orders[0];
		Assert.AreEqual("ORD-001", firstOrder.Id.Id);
		Assert.AreEqual("USER-A", firstOrder.CustomerId.Value);
		Assert.HasCount(2, firstOrder.Products);
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
		Assert.HasCount(10, deserialized);

		ProductCode firstProduct = ProductCode.Parse("PROD-0");
		Assert.IsTrue(deserialized.ContainsKey(firstProduct));
		Assert.HasCount(100, deserialized[firstProduct]);
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
		Assert.HasCount(6, deserialized);

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
