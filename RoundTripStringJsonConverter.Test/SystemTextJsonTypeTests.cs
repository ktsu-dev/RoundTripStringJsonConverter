// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SystemTextJsonTypeTests
{
	public class NodeHolder
	{
		public JsonNode? Node { get; set; }
	}

	public class DocumentHolder
	{
		public JsonDocument? Document { get; set; }
	}

	private static JsonSerializerOptions GetOptions()
	{
		JsonSerializerOptions options = new();
		options.Converters.Add(new RoundTripStringJsonConverterFactory());
		return options;
	}

	[TestMethod]
	[DataRow(typeof(JsonNode))]
	[DataRow(typeof(JsonObject))]
	[DataRow(typeof(JsonArray))]
	[DataRow(typeof(JsonValue))]
	[DataRow(typeof(JsonDocument))]
	[DataRow(typeof(JsonElement))]
	public void CanConvertShouldRejectSystemTextJsonTypes(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(type), $"{type.Name} should be left to System.Text.Json's own converter");
	}

	[TestMethod]
	public void JsonNodePropertyShouldSerializeAsAnObject()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize(new NodeHolder { Node = JsonNode.Parse("{\"a\":1}") }, options);

		Assert.AreEqual("{\"Node\":{\"a\":1}}", json);
	}

	[TestMethod]
	public void JsonNodePropertyShouldDeserializeFromAnObject()
	{
		JsonSerializerOptions options = GetOptions();

		NodeHolder? result = JsonSerializer.Deserialize<NodeHolder>("{\"Node\":{\"a\":1}}", options);

		Assert.IsNotNull(result);
		Assert.IsNotNull(result.Node);
		Assert.AreEqual(1, result.Node["a"]!.GetValue<int>());
	}

	[TestMethod]
	public void JsonDocumentPropertyShouldRoundTripAsAnObject()
	{
		JsonSerializerOptions options = GetOptions();

		DocumentHolder? result = JsonSerializer.Deserialize<DocumentHolder>("{\"Document\":{\"a\":1}}", options);
		Assert.IsNotNull(result);
		Assert.IsNotNull(result.Document);
		string json = JsonSerializer.Serialize(result, options);
		result.Document.Dispose();

		Assert.AreEqual("{\"Document\":{\"a\":1}}", json);
	}
}
