// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter;

using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// A factory for creating JSON converters that use a type's ToString and string conversion methods for serialization.
/// Supports various string conversion methods including FromString, Parse, Create, and Convert.
/// </summary>
public class RoundTripStringJsonConverterFactory : JsonConverterFactory
{
	/// <summary>
	/// Supported method names for string conversion, in order of preference.
	/// </summary>
	private static readonly string[] SupportedMethodNames = ["FromString", "Parse", "Create", "Convert"];

	/// <summary>
	/// Finds a suitable string conversion method for the specified type.
	/// </summary>
	/// <param name="type">The type to check for conversion methods.</param>
	/// <returns>The method info if found, null otherwise.</returns>
	private static MethodInfo? FindStringConversionMethod(Type type)
	{
		foreach (string methodName in SupportedMethodNames)
		{
			try
			{
				// Get all methods with the specified name and binding flags
				MethodInfo[] methods = [.. type.GetMethods(BindingFlags.Static | BindingFlags.Public)
					.Where(m => m.Name == methodName)];

				// Find the first method that matches our criteria
				foreach (MethodInfo method in methods)
				{
					ParameterInfo[] parameters = method.GetParameters();
					if (parameters.Length > 0 &&
						parameters[0].ParameterType == typeof(string) &&
						(method.ReturnType == type ||
						 (method.ReturnType.IsGenericType && type.IsGenericType &&
						  method.ReturnType.GetGenericTypeDefinition() == type.GetGenericTypeDefinition())))
					{
						return method;
					}
				}
			}
			catch (AmbiguousMatchException)
			{
				// If there's an ambiguous match, try to find the specific overload we want
				try
				{
					MethodInfo? method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public, null, [typeof(string)], null);
					if (method is not null &&
						(method.ReturnType == type ||
						 (method.ReturnType.IsGenericType && type.IsGenericType &&
						  method.ReturnType.GetGenericTypeDefinition() == type.GetGenericTypeDefinition())))
					{
						return method;
					}
				}
				catch (ArgumentException)
				{
					// Continue to next method name if this one fails
					continue;
				}
				catch (AmbiguousMatchException)
				{
					// Continue to next method name if this one fails
					continue;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Determines whether the specified type can be converted by this factory.
	/// </summary>
	/// <param name="typeToConvert">The type to check for conversion capability.</param>
	/// <returns>True if the type can be converted; otherwise, false.</returns>
	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);
		return FindStringConversionMethod(typeToConvert) is not null;
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
		Type converterType = typeof(RoundTripStringJsonConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter)Activator.CreateInstance(converterType, BindingFlags.Instance | BindingFlags.Public, binder: null, args: null, culture: null)!;
	}

	/// <summary>
	/// JSON converter that uses a type's ToString and string conversion methods for serialization.
	/// Supports various string conversion methods including FromString, Parse, Create, and Convert.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	private sealed class RoundTripStringJsonConverter<T> : JsonConverter<T>
	{
		private static readonly MethodInfo? StringConversionMethod;

		static RoundTripStringJsonConverter()
		{
			StringConversionMethod = FindStringConversionMethod(typeof(T));
			if (StringConversionMethod is not null && StringConversionMethod.ContainsGenericParameters)
			{
				StringConversionMethod = StringConversionMethod.MakeGenericMethod(typeof(T));
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
				? (T)StringConversionMethod!.Invoke(null, [reader.GetString()!])!
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

			return (T)StringConversionMethod!.Invoke(null, [reader.GetString()!])!;
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
