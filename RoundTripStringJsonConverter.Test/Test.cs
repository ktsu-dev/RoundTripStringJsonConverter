// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

namespace ktsu.RoundTripStringJsonConverter.Test;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Test
{
	public static Test FromString(string value) => new(value);

	public override string ToString() => hiddenString;

	private readonly string hiddenString;

	public Test(string value) => hiddenString = value;
	public Test() => hiddenString = "default";

	private static JsonSerializerOptions JsonSerializerOptions { get; } = new(JsonSerializerDefaults.General)
	{
		Converters =
		{
			new RoundTripStringJsonConverterFactory(),
		}
	};

	[TestMethod]
	public void TestRoundTrip()
	{
		Test test = new("test");
		string jsonString = JsonSerializer.Serialize(test, JsonSerializerOptions);
		Test? result = JsonSerializer.Deserialize<Test>(jsonString, JsonSerializerOptions);
		Assert.IsNotNull(result);
		Assert.AreEqual(test.hiddenString, result.hiddenString);
	}

	[TestMethod]
	public void TestDictionary()
	{
		Dictionary<Test, int> test = new()
		{
			{
				new("test1"), 1
			},
			{
				new("test2"), 2
			},
		};
		string jsonString = JsonSerializer.Serialize(test, JsonSerializerOptions);
		Dictionary<Test, int> result = JsonSerializer.Deserialize<Dictionary<Test, int>>(jsonString, JsonSerializerOptions) ?? [];
		Assert.IsTrue(test.Keys.Select(x => x.hiddenString).SequenceEqual(result.Keys.Select(x => x.hiddenString)));
		Assert.IsTrue(test.Values.SequenceEqual(result.Values));
	}
}
