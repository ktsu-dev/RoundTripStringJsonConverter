// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using ktsu.Extensions;

/// <summary>
/// A factory for creating JSON converters that use a type's ToString and FromString methods for serialization.
/// </summary>
public class RoundTripStringJsonConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Determines whether the specified type can be converted by this factory.
	/// </summary>
	/// <param name="typeToConvert">The type to check for conversion capability.</param>
	/// <returns>True if the type can be converted; otherwise, false.</returns>
	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return typeToConvert.TryFindMethod("FromString", BindingFlags.Static | BindingFlags.Public, out var method) &&
			   method is not null && method.GetParameters().Length != 0 &&
			   method.GetParameters()[0].ParameterType == typeof(string);
	}

	/// <summary>
	/// Creates a JSON converter for the specified type.
	/// </summary>
	/// <param name="typeToConvert">The type to create a converter for.</param>
	/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
	/// <returns>A JSON converter for the specified type.</returns>
	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		var converterType = typeof(RoundTripStringJsonConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter)Activator.CreateInstance(converterType, BindingFlags.Instance | BindingFlags.Public, binder: null, args: null, culture: null)!;
	}

	/// <summary>
	/// JSON converter that uses a type's ToString and FromString methods for serialization.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	private sealed class RoundTripStringJsonConverter<T> : JsonConverter<T>
	{
		private static readonly MethodInfo? FromStringMethod;

		static RoundTripStringJsonConverter()
		{
			if (typeof(T).TryFindMethod("FromString", BindingFlags.Static | BindingFlags.Public, out FromStringMethod))
			{
				Debug.Assert(FromStringMethod is not null);
				if (FromStringMethod.ContainsGenericParameters)
				{
					FromStringMethod = FromStringMethod.MakeGenericMethod(typeof(T));
				}
			}
		}

		/// <summary>
		/// Reads and converts the JSON to the specified type.
		/// </summary>
		/// <param name="reader">The reader to read the JSON from.</param>
		/// <param name="typeToConvert">The type to convert to.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		/// <returns>The converted value.</returns>
		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(typeToConvert);
			return reader.TokenType == JsonTokenType.String
				? (T)FromStringMethod!.Invoke(null, [reader.GetString()!])!
				: throw new JsonException();
		}

		/// <summary>
		/// Reads and converts the JSON to the specified type as a property name.
		/// </summary>
		/// <param name="reader">The reader to read the JSON from.</param>
		/// <param name="typeToConvert">The type to convert to.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		/// <returns>The converted value.</returns>
		public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(typeToConvert);

			return (T)FromStringMethod!.Invoke(null, [reader.GetString()!])!;
		}

		/// <summary>
		/// Writes the specified value as JSON.
		/// </summary>
		/// <param name="writer">The writer to write the JSON to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(value);
			ArgumentNullException.ThrowIfNull(writer);
			writer.WriteStringValue(value.ToString());
		}

		/// <summary>
		/// Writes the specified value as a JSON property name.
		/// </summary>
		/// <param name="writer">The writer to write the JSON to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			ArgumentNullException.ThrowIfNull(value);
			ArgumentNullException.ThrowIfNull(writer);
			writer.WritePropertyName(value.ToString()!);
		}
	}
}
