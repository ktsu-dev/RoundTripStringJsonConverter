// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter.Test;

using System.Text.Json;

[TestClass]
public class SimpleTest
{
	[TestMethod]
	public void TestMultipleConversionMethods()
	{
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			Converters = { new RoundTripStringJsonConverterFactory() }
		};

		// Test FromString
		DemoExample.PersonId personId = DemoExample.PersonId.FromString("person123");
		string personJson = JsonSerializer.Serialize(personId, options);
		DemoExample.PersonId? deserializedPersonId = JsonSerializer.Deserialize<DemoExample.PersonId>(personJson, options);
		Assert.AreEqual("person123", deserializedPersonId?.Value);

		// Test Parse
		DemoExample.ProductCode productCode = DemoExample.ProductCode.Parse("PROD456");
		string productJson = JsonSerializer.Serialize(productCode, options);
		DemoExample.ProductCode? deserializedProductCode = JsonSerializer.Deserialize<DemoExample.ProductCode>(productJson, options);
		Assert.AreEqual("PROD456", deserializedProductCode?.Code);

		// Test Create
		DemoExample.OrderId orderId = DemoExample.OrderId.Create("ORDER789");
		string orderJson = JsonSerializer.Serialize(orderId, options);
		DemoExample.OrderId? deserializedOrderId = JsonSerializer.Deserialize<DemoExample.OrderId>(orderJson, options);
		Assert.AreEqual("ORDER789", deserializedOrderId?.Id);

		// Test Convert
		DemoExample.CategoryName categoryName = DemoExample.CategoryName.Convert("Electronics");
		string categoryJson = JsonSerializer.Serialize(categoryName, options);
		DemoExample.CategoryName? deserializedCategoryName = JsonSerializer.Deserialize<DemoExample.CategoryName>(categoryJson, options);
		Assert.AreEqual("Electronics", deserializedCategoryName?.Name);
	}
}
