// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Runs against the netstandard2.0 build of the library, which .NET Framework, Unity and Mono consumers get.
/// Every other test runs against the net10.0 build. The asset is copied next to the tests by the test project.
/// </summary>
[TestClass]
public class NetStandardAssetTests
{
	public sealed record Key(string V)
	{
		public static Key FromString(string s) => new(s);

		public override string ToString() => V;
	}

	private static JsonSerializerOptions CreateOptionsWithNetStandardFactory(out AssemblyLoadContext context)
	{
		string assetPath = Path.Combine(AppContext.BaseDirectory, "netstandard2.0", "ktsu.RoundTripStringJsonConverter.dll");
		Assert.IsTrue(File.Exists(assetPath), $"The netstandard2.0 asset was not copied to {assetPath}");

		// System.Text.Json and the other dependencies resolve from the default context, so the factory shares
		// JsonConverterFactory with the serializer used here.
		context = new AssemblyLoadContext(nameof(NetStandardAssetTests), isCollectible: true);
		Assembly asset = context.LoadFromAssemblyPath(assetPath);
		Assert.AreEqual(".NETStandard,Version=v2.0", asset.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName);

		Type factoryType = asset.GetType("ktsu.RoundTripStringJsonConverter.RoundTripStringJsonConverterFactory", throwOnError: true)!;
		JsonConverter factory = (JsonConverter)Activator.CreateInstance(factoryType)!;
		return new JsonSerializerOptions { Converters = { factory } };
	}

	[TestMethod]
	public void NetStandardAsset_DictionaryKey_RoundTrips()
	{
		JsonSerializerOptions options = CreateOptionsWithNetStandardFactory(out AssemblyLoadContext context);
		try
		{
			Dictionary<Key, int> original = new() { [new Key("a")] = 1, [new Key("b")] = 2 };

			string json = JsonSerializer.Serialize(original, options);
			Dictionary<Key, int>? back = JsonSerializer.Deserialize<Dictionary<Key, int>>(json, options);

			Assert.AreEqual("{\"a\":1,\"b\":2}", json);
			Assert.IsNotNull(back);
			Assert.AreEqual(2, back.Count);
			Assert.AreEqual(1, back[new Key("a")]);
			Assert.AreEqual(2, back[new Key("b")]);
		}
		finally
		{
			context.Unload();
		}
	}

	[TestMethod]
	public void NetStandardAsset_Value_RoundTrips()
	{
		JsonSerializerOptions options = CreateOptionsWithNetStandardFactory(out AssemblyLoadContext context);
		try
		{
			string json = JsonSerializer.Serialize(new Key("a"), options);
			Key? back = JsonSerializer.Deserialize<Key>(json, options);

			Assert.AreEqual("\"a\"", json);
			Assert.AreEqual(new Key("a"), back);
		}
		finally
		{
			context.Unload();
		}
	}
}
