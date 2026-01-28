// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ktsu.Semantics.Strings;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SemanticStringTests
{
	// Define various SemanticString-derived types for testing
	public sealed record EmailAddress : SemanticString<EmailAddress>
	{
	}

	public sealed record PhoneNumber : SemanticString<PhoneNumber>
	{
	}

	public sealed record CustomerName : SemanticString<CustomerName>
	{
	}

	public sealed record ProductSku : SemanticString<ProductSku>
	{
	}

	public sealed record ApiKey : SemanticString<ApiKey>
	{
	}

	// Test classes using SemanticString types
	public class Customer
	{
		public CustomerName Name { get; set; } = null!;
		public EmailAddress Email { get; set; } = null!;
		public PhoneNumber? Phone { get; set; }
	}

	public class Product
	{
		public ProductSku Sku { get; set; } = null!;
		public CustomerName Name { get; set; } = null!; // Reusing CustomerName for product names
		public decimal Price { get; set; }
	}

	public class ApiConfiguration
	{
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IDictionary<ApiKey, string> Keys { get; set; } = new Dictionary<ApiKey, string>();
		[SuppressMessage("Design", "CA2227:Collection properties should be read only", Justification = "Required for JSON deserialization")]
		public IList<EmailAddress> NotificationEmails { get; set; } = [];
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
	public void Should_Serialize_And_Deserialize_Basic_SemanticString_Types()
	{
		JsonSerializerOptions options = GetOptions();

		// Test EmailAddress
		EmailAddress email = EmailAddress.Create("user@example.com");
		string emailJson = JsonSerializer.Serialize(email, options);
		EmailAddress? deserializedEmail = JsonSerializer.Deserialize<EmailAddress>(emailJson, options);

		Assert.IsNotNull(deserializedEmail);
		Assert.AreEqual("user@example.com", deserializedEmail.ToString());

		// Test PhoneNumber
		PhoneNumber phone = PhoneNumber.Create("+1-555-123-4567");
		string phoneJson = JsonSerializer.Serialize(phone, options);
		PhoneNumber? deserializedPhone = JsonSerializer.Deserialize<PhoneNumber>(phoneJson, options);

		Assert.IsNotNull(deserializedPhone);
		Assert.AreEqual("+1-555-123-4567", deserializedPhone.ToString());

		// Test CustomerName
		CustomerName name = CustomerName.Create("John Doe");
		string nameJson = JsonSerializer.Serialize(name, options);
		CustomerName? deserializedName = JsonSerializer.Deserialize<CustomerName>(nameJson, options);

		Assert.IsNotNull(deserializedName);
		Assert.AreEqual("John Doe", deserializedName.ToString());
	}

	[TestMethod]
	public void Should_Handle_Complex_Objects_With_SemanticString_Properties()
	{
		JsonSerializerOptions options = GetOptions();

		Customer original = new()
		{
			Name = CustomerName.Create("Alice Smith"),
			Email = EmailAddress.Create("alice@company.com"),
			Phone = PhoneNumber.Create("(555) 987-6543")
		};

		string json = JsonSerializer.Serialize(original, options);
		Customer? deserialized = JsonSerializer.Deserialize<Customer>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("Alice Smith", deserialized.Name.ToString());
		Assert.AreEqual("alice@company.com", deserialized.Email.ToString());
		Assert.IsNotNull(deserialized.Phone);
		Assert.AreEqual("(555) 987-6543", deserialized.Phone.ToString());
	}

	[TestMethod]
	public void Should_Handle_Nullable_SemanticString_Properties()
	{
		JsonSerializerOptions options = GetOptions();

		Customer original = new()
		{
			Name = CustomerName.Create("Bob Johnson"),
			Email = EmailAddress.Create("bob@example.com"),
			Phone = null // Nullable property
		};

		string json = JsonSerializer.Serialize(original, options);
		Customer? deserialized = JsonSerializer.Deserialize<Customer>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("Bob Johnson", deserialized.Name.ToString());
		Assert.AreEqual("bob@example.com", deserialized.Email.ToString());
		Assert.IsNull(deserialized.Phone);
	}

	[TestMethod]
	public void Should_Handle_Collections_Of_SemanticString_Types()
	{
		JsonSerializerOptions options = GetOptions();

		List<EmailAddress> emailList = [
			EmailAddress.Create("first@example.com"),
			EmailAddress.Create("second@example.com"),
			EmailAddress.Create("third@example.com")
		];

		string json = JsonSerializer.Serialize(emailList, options);
		List<EmailAddress>? deserializedList = JsonSerializer.Deserialize<List<EmailAddress>>(json, options);

		Assert.IsNotNull(deserializedList);
		Assert.HasCount(3, deserializedList);
		Assert.AreEqual("first@example.com", deserializedList[0].ToString());
		Assert.AreEqual("second@example.com", deserializedList[1].ToString());
		Assert.AreEqual("third@example.com", deserializedList[2].ToString());
	}

	[TestMethod]
	public void Should_Handle_SemanticString_Types_As_Dictionary_Keys()
	{
		JsonSerializerOptions options = GetOptions();

		Dictionary<ProductSku, Product> productCatalog = new()
		{
			{
				ProductSku.Create("SKU-001"),
				new Product { Sku = ProductSku.Create("SKU-001"), Name = CustomerName.Create("Laptop"), Price = 999.99m }
			},
			{
				ProductSku.Create("SKU-002"),
				new Product { Sku = ProductSku.Create("SKU-002"), Name = CustomerName.Create("Mouse"), Price = 29.99m }
			}
		};

		string json = JsonSerializer.Serialize(productCatalog, options);
		Dictionary<ProductSku, Product>? deserializedCatalog = JsonSerializer.Deserialize<Dictionary<ProductSku, Product>>(json, options);

		Assert.IsNotNull(deserializedCatalog);
		Assert.HasCount(2, deserializedCatalog);

		ProductSku sku1 = ProductSku.Create("SKU-001");
		ProductSku sku2 = ProductSku.Create("SKU-002");

		Assert.IsTrue(deserializedCatalog.ContainsKey(sku1));
		Assert.IsTrue(deserializedCatalog.ContainsKey(sku2));

		Assert.AreEqual("Laptop", deserializedCatalog[sku1].Name.ToString());
		Assert.AreEqual(999.99m, deserializedCatalog[sku1].Price);

		Assert.AreEqual("Mouse", deserializedCatalog[sku2].Name.ToString());
		Assert.AreEqual(29.99m, deserializedCatalog[sku2].Price);
	}

	[TestMethod]
	public void Should_Handle_Nested_Collections_With_SemanticString_Types()
	{
		JsonSerializerOptions options = GetOptions();

		ApiConfiguration config = new();
		config.Keys.Add(ApiKey.Create("api-key-1"), "Development Environment");
		config.Keys.Add(ApiKey.Create("api-key-2"), "Production Environment");
		config.Keys.Add(ApiKey.Create("api-key-3"), "Testing Environment");

		config.NotificationEmails.Add(EmailAddress.Create("dev@company.com"));
		config.NotificationEmails.Add(EmailAddress.Create("ops@company.com"));
		config.NotificationEmails.Add(EmailAddress.Create("admin@company.com"));

		string json = JsonSerializer.Serialize(config, options);
		ApiConfiguration? deserializedConfig = JsonSerializer.Deserialize<ApiConfiguration>(json, options);

		Assert.IsNotNull(deserializedConfig);
		Assert.HasCount(3, deserializedConfig.Keys);
		Assert.HasCount(3, deserializedConfig.NotificationEmails);

		// Test dictionary keys and values
		ApiKey testKey = ApiKey.Create("api-key-1");
		Assert.IsTrue(deserializedConfig.Keys.ContainsKey(testKey));
		Assert.AreEqual("Development Environment", deserializedConfig.Keys[testKey]);

		// Test list contents
		Assert.AreEqual("dev@company.com", deserializedConfig.NotificationEmails[0].ToString());
		Assert.AreEqual("ops@company.com", deserializedConfig.NotificationEmails[1].ToString());
		Assert.AreEqual("admin@company.com", deserializedConfig.NotificationEmails[2].ToString());
	}

	[TestMethod]
	public void Should_Handle_Empty_SemanticString_Values()
	{
		JsonSerializerOptions options = GetOptions();

		// Test empty string
		CustomerName emptyName = CustomerName.Create("");
		string json = JsonSerializer.Serialize(emptyName, options);
		CustomerName? deserialized = JsonSerializer.Deserialize<CustomerName>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("", deserialized.ToString());
	}

	[TestMethod]
	public void Should_Handle_SemanticString_With_Special_Characters()
	{
		JsonSerializerOptions options = GetOptions();

		// Test various special characters
		CustomerName specialName = CustomerName.Create("John \"The Great\" O'Connor & Co. <test@example.com>");
		string json = JsonSerializer.Serialize(specialName, options);
		CustomerName? deserialized = JsonSerializer.Deserialize<CustomerName>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual("John \"The Great\" O'Connor & Co. <test@example.com>", deserialized.ToString());

		// Test Unicode characters
		CustomerName unicodeName = CustomerName.Create("José María González 测试用户 🎉");
		string unicodeJson = JsonSerializer.Serialize(unicodeName, options);
		CustomerName? deserializedUnicode = JsonSerializer.Deserialize<CustomerName>(unicodeJson, options);

		Assert.IsNotNull(deserializedUnicode);
		Assert.AreEqual("José María González 测试用户 🎉", deserializedUnicode.ToString());
	}

	[TestMethod]
	public void Should_Maintain_Equality_After_Serialization()
	{
		JsonSerializerOptions options = GetOptions();

		EmailAddress original = EmailAddress.Create("test@example.com");
		string json = JsonSerializer.Serialize(original, options);
		EmailAddress? deserialized = JsonSerializer.Deserialize<EmailAddress>(json, options);

		Assert.IsNotNull(deserialized);
		Assert.AreEqual(original, deserialized);
		Assert.AreEqual(original.GetHashCode(), deserialized.GetHashCode());
	}

	[TestMethod]
	public void Should_Handle_Large_Collections_Of_SemanticString_Types()
	{
		JsonSerializerOptions options = GetOptions();

		// Create a large collection
		List<ProductSku> largeSkuList = [];
		for (int i = 0; i < 1000; i++)
		{
			largeSkuList.Add(ProductSku.Create($"SKU-{i:D6}"));
		}

		string json = JsonSerializer.Serialize(largeSkuList, options);
		List<ProductSku>? deserializedList = JsonSerializer.Deserialize<List<ProductSku>>(json, options);

		Assert.IsNotNull(deserializedList);
		Assert.HasCount(1000, deserializedList);
		Assert.AreEqual("SKU-000000", deserializedList[0].ToString());
		Assert.AreEqual("SKU-000999", deserializedList[999].ToString());
	}

	[TestMethod]
	public void Should_Handle_Mixed_SemanticString_Types_In_Same_Collection()
	{
		JsonSerializerOptions options = GetOptions();

		// Create a dictionary with different SemanticString types as values
		Dictionary<string, object> mixedValues = new()
		{
			{ "email", EmailAddress.Create("user@test.com") },
			{ "phone", PhoneNumber.Create("555-1234") },
			{ "name", CustomerName.Create("Test User") },
			{ "sku", ProductSku.Create("TEST-001") },
			{ "regular_string", "Just a string" },
			{ "number", 42 }
		};

		string json = JsonSerializer.Serialize(mixedValues, options);
		Dictionary<string, object>? deserializedValues = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

		Assert.IsNotNull(deserializedValues);
		Assert.HasCount(6, deserializedValues);

		// Note: Due to JSON serialization limitations, custom types in mixed collections
		// may be serialized as strings, but the conversion should not fail
		Assert.IsTrue(deserializedValues.ContainsKey("email"));
		Assert.IsTrue(deserializedValues.ContainsKey("phone"));
		Assert.IsTrue(deserializedValues.ContainsKey("name"));
		Assert.IsTrue(deserializedValues.ContainsKey("sku"));
	}
}
