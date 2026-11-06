// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// An abstract base or an interface with a static conversion method is converted even though it doesn't
/// override ToString itself: the value written is always a derived type, whose override supplies the string.
/// </summary>
[TestClass]
public class AbstractDeclaredTypeTests
{
	public abstract class Shape
	{
		public static Shape Parse(string s) => s == "circle" ? new Circle() : new Square();
	}

	public sealed class Circle : Shape
	{
		public override string ToString() => "circle";
	}

	public sealed class Square : Shape
	{
		public override string ToString() => "square";
	}

	public interface IShape
	{
		public static IShape Parse(string s) => new Triangle(s);
	}

	public sealed class Triangle(string name) : IShape
	{
		public string Name { get; } = name;

		public override string ToString() => Name;
	}

	public sealed class Holder
	{
		public Shape? S { get; set; }

		public IShape? I { get; set; }
	}

	private static JsonSerializerOptions GetOptions() => new() { Converters = { new RoundTripStringJsonConverterFactory() } };

	[TestMethod]
	public void CanConvertShouldAcceptAbstractBaseAndInterface()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsTrue(factory.CanConvert(typeof(Shape)));
		Assert.IsTrue(factory.CanConvert(typeof(IShape)));
	}

	[TestMethod]
	public void AbstractBaseShouldRoundTripAsRootValue()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize<Shape>(new Circle(), options);
		Shape? shape = JsonSerializer.Deserialize<Shape>(json, options);

		Assert.AreEqual("\"circle\"", json);
		Assert.IsInstanceOfType<Circle>(shape);
	}

	[TestMethod]
	public void InterfaceShouldRoundTripAsRootValue()
	{
		JsonSerializerOptions options = GetOptions();

		string json = JsonSerializer.Serialize<IShape>(new Triangle("tri"), options);
		IShape? shape = JsonSerializer.Deserialize<IShape>(json, options);

		Assert.AreEqual("\"tri\"", json);
		Assert.IsInstanceOfType<Triangle>(shape);
		Assert.AreEqual("tri", ((Triangle)shape).Name);
	}

	[TestMethod]
	public void AbstractBaseAndInterfaceShouldRoundTripAsProperties()
	{
		JsonSerializerOptions options = GetOptions();
		Holder holder = new() { S = new Square(), I = new Triangle("tri") };

		string json = JsonSerializer.Serialize(holder, options);
		Holder? result = JsonSerializer.Deserialize<Holder>(json, options);

		Assert.AreEqual("{\"S\":\"square\",\"I\":\"tri\"}", json);
		Assert.IsNotNull(result);
		Assert.IsInstanceOfType<Square>(result.S);
		Assert.AreEqual("tri", result.I?.ToString());
	}

	[TestMethod]
	public void AbstractBaseShouldRoundTripAsDictionaryKey()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<Shape, int> dictionary = new() { [new Circle()] = 1 };

		string json = JsonSerializer.Serialize(dictionary, options);
		Dictionary<Shape, int>? result = JsonSerializer.Deserialize<Dictionary<Shape, int>>(json, options);

		Assert.AreEqual("{\"circle\":1}", json);
		Assert.IsNotNull(result);
		KeyValuePair<Shape, int> entry = result.Single();
		Assert.IsInstanceOfType<Circle>(entry.Key);
		Assert.AreEqual(1, entry.Value);
	}

	[TestMethod]
	public void InterfaceShouldRoundTripAsDictionaryKey()
	{
		JsonSerializerOptions options = GetOptions();
		Dictionary<IShape, int> dictionary = new() { [new Triangle("tri")] = 1 };

		string json = JsonSerializer.Serialize(dictionary, options);
		Dictionary<IShape, int>? result = JsonSerializer.Deserialize<Dictionary<IShape, int>>(json, options);

		Assert.AreEqual("{\"tri\":1}", json);
		Assert.IsNotNull(result);
		KeyValuePair<IShape, int> entry = result.Single();
		Assert.AreEqual("tri", entry.Key.ToString());
		Assert.AreEqual(1, entry.Value);
	}
}
