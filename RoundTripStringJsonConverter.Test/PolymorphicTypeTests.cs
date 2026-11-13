// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// A type with System.Text.Json polymorphism configured through [JsonPolymorphic] or [JsonDerivedType] is
/// left to System.Text.Json, whose polymorphic resolver throws once a custom converter claims the type.
/// </summary>
[TestClass]
public class PolymorphicTypeTests
{
	[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
	[JsonDerivedType(typeof(Circle), "circle")]
	[JsonDerivedType(typeof(Square), "square")]
	public abstract class Shape
	{
		public static Shape Create(string kind) => kind == "circle" ? new Circle() : new Square();
	}

	public sealed class Circle : Shape
	{
		public double Radius { get; set; }

		public override string ToString() => $"Circle r={Radius}";
	}

	public sealed class Square : Shape
	{
		public double Side { get; set; }

		public override string ToString() => $"Square s={Side}";
	}

	[JsonDerivedType(typeof(Dog), "dog")]
	public class Animal
	{
		public string Name { get; set; } = string.Empty;

		public static Animal Parse(string name) => new() { Name = name };

		public override string ToString() => Name;
	}

	public sealed class Dog : Animal
	{
		public int Barks { get; set; } = 3;
	}

	public sealed class Holder
	{
		public Shape? S { get; set; }

		public Animal? A { get; set; }
	}

	private static JsonSerializerOptions GetOptions() => new() { Converters = { new RoundTripStringJsonConverterFactory() } };

	[TestMethod]
	public void CanConvertShouldDeclinePolymorphicTypes()
	{
		RoundTripStringJsonConverterFactory factory = new();

		Assert.IsFalse(factory.CanConvert(typeof(Shape)));
		Assert.IsFalse(factory.CanConvert(typeof(Animal)));
	}

	[TestMethod]
	public void PolymorphicRootValuesShouldMatchSystemTextJson()
	{
		JsonSerializerOptions options = GetOptions();
		Shape circle = new Circle { Radius = 5 };
		Animal dog = new Dog { Name = "Rex" };

		string shapeJson = JsonSerializer.Serialize(circle, options);
		string animalJson = JsonSerializer.Serialize(dog, options);

		Assert.AreEqual(JsonSerializer.Serialize(circle), shapeJson);
		Assert.AreEqual(JsonSerializer.Serialize(dog), animalJson);
		Assert.AreEqual(5, Assert.IsInstanceOfType<Circle>(JsonSerializer.Deserialize<Shape>(shapeJson, options)).Radius);
		Dog readDog = Assert.IsInstanceOfType<Dog>(JsonSerializer.Deserialize<Animal>(animalJson, options));
		Assert.AreEqual("Rex", readDog.Name);
	}

	[TestMethod]
	public void PolymorphicPropertiesShouldMatchSystemTextJson()
	{
		JsonSerializerOptions options = GetOptions();
		Holder holder = new() { S = new Square { Side = 2 }, A = new Dog { Name = "Rex", Barks = 7 } };

		string json = JsonSerializer.Serialize(holder, options);
		Holder? read = JsonSerializer.Deserialize<Holder>(json, options);

		Assert.AreEqual(JsonSerializer.Serialize(holder), json);
		Assert.IsNotNull(read);
		Assert.AreEqual(2, Assert.IsInstanceOfType<Square>(read.S).Side);
		Assert.AreEqual(7, Assert.IsInstanceOfType<Dog>(read.A).Barks);
	}

	[TestMethod]
	public void PolymorphicListsShouldMatchSystemTextJson()
	{
		JsonSerializerOptions options = GetOptions();
		List<Shape> shapes = [new Circle { Radius = 1 }, new Square { Side = 3 }];

		string json = JsonSerializer.Serialize(shapes, options);
		List<Shape>? read = JsonSerializer.Deserialize<List<Shape>>(json, options);

		Assert.AreEqual(JsonSerializer.Serialize(shapes), json);
		Assert.IsNotNull(read);
		Assert.IsInstanceOfType<Circle>(read[0]);
		Assert.AreEqual(3, Assert.IsInstanceOfType<Square>(read[1]).Side);
	}
}
