// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.RoundTripStringJsonConverter;

using System.Collections.Generic;
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
	/// Built-in types that should not be converted by this converter.
	/// </summary>
	private static readonly HashSet<Type> BuiltInTypes = [
		typeof(string),
		typeof(int),
		typeof(long),
		typeof(double),
		typeof(decimal),
		typeof(bool),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(Guid),
		typeof(TimeSpan),
		typeof(object),
		typeof(Array),
		typeof(byte),
		typeof(sbyte),
		typeof(short),
		typeof(ushort),
		typeof(uint),
		typeof(ulong),
		typeof(float),
		typeof(char),
		typeof(IntPtr),
		typeof(UIntPtr)
	];

	/// <summary>
	/// Determines if a type is a built-in type that should not be converted.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is a built-in type; otherwise, false.</returns>
	private static bool IsBuiltInType(Type type)
	{
		// Check exact type match
		if (BuiltInTypes.Contains(type))
		{
			return true;
		}

		// Check if it's a system type
		if (type.Namespace == "System" && type.Assembly == typeof(string).Assembly)
		{
			return true;
		}

		// Check if it's a generic collection type
		if (type.IsGenericType)
		{
			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			if (genericTypeDefinition == typeof(List<>) ||
				genericTypeDefinition == typeof(Dictionary<,>) ||
				genericTypeDefinition == typeof(IList<>) ||
				genericTypeDefinition == typeof(ICollection<>) ||
				genericTypeDefinition == typeof(IEnumerable<>) ||
				genericTypeDefinition == typeof(IDictionary<,>) ||
				genericTypeDefinition == typeof(Nullable<>))
			{
				return true;
			}
		}

		// Check if it's an array
		return type.IsArray;
	}

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
				MethodInfo[] publicStaticMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				// Get all methods with the specified name and binding flags
				MethodInfo[] methods = [.. publicStaticMethods
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
		Guard.NotNull(typeToConvert, nameof(typeToConvert));

		// Don't convert built-in types
		if (IsBuiltInType(typeToConvert))
		{
			return false;
		}

		// Check if we can find a string conversion method for this type
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
		Guard.NotNull(typeToConvert, nameof(typeToConvert));
		Guard.NotNull(options, nameof(options));
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
		private static readonly MethodInfo? StringConversionMethod = CreateStringConversionMethod();

		private static MethodInfo? CreateStringConversionMethod()
		{
			MethodInfo? method = FindStringConversionMethod(typeof(T));
			if (method is not null && method.ContainsGenericParameters)
			{
				method = method.MakeGenericMethod(typeof(T));
			}
			return method;
		}

		/// <summary>
		/// Gets a value indicating whether null values should be handled by this converter.
		/// </summary>
		public override bool HandleNull => true;

		/// <summary>
		/// Reads and converts the JSON to the specified type.
		/// </summary>
		/// <param name="reader">The reader to read the JSON from.</param>
		/// <param name="typeToConvert">The type to convert to.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		/// <returns>The converted value.</returns>
		public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Guard.NotNull(typeToConvert, nameof(typeToConvert));

			if (reader.TokenType == JsonTokenType.Null)
			{
				return default;
			}

			if (reader.TokenType != JsonTokenType.String)
			{
				throw new JsonException($"Expected string token, got {reader.TokenType}");
			}

			string? stringValue = reader.GetString();
			Guard.NotNull(stringValue, nameof(stringValue));

			try
			{
				return (T)StringConversionMethod!.Invoke(null, [stringValue])!;
			}
			catch (TargetInvocationException ex) when (ex.InnerException is not null)
			{
				// Unwrap the inner exception to preserve the original exception type
				throw ex.InnerException;
			}
		}

#if NET6_0_OR_GREATER
		/// <summary>
		/// Reads and converts the JSON to the specified type as a property name.
		/// </summary>
		/// <param name="reader">The reader to read the JSON from.</param>
		/// <param name="typeToConvert">The type to convert to.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		/// <returns>The converted value.</returns>
		public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			Guard.NotNull(typeToConvert, nameof(typeToConvert));

			string? stringValue = reader.GetString();
			Guard.NotNull(stringValue, nameof(stringValue));

			try
			{
				return (T)StringConversionMethod!.Invoke(null, [stringValue])!;
			}
			catch (TargetInvocationException ex) when (ex.InnerException is not null)
			{
				// Unwrap the inner exception to preserve the original exception type
				throw ex.InnerException;
			}
		}
#endif

		/// <summary>
		/// Writes the specified value as JSON.
		/// </summary>
		/// <param name="writer">The writer to write the JSON to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			Guard.NotNull(writer, nameof(writer));
			if (value is null)
			{
				writer.WriteNullValue();
				return;
			}

			string? stringValue = value.ToString();
			writer.WriteStringValue(stringValue);
		}

#if NET6_0_OR_GREATER
		/// <summary>
		/// Writes the specified value as a JSON property name.
		/// </summary>
		/// <param name="writer">The writer to write the JSON to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="options">Options to control the behavior during serialization and deserialization.</param>
		public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			Guard.NotNull(writer, nameof(writer));

			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			string? stringValue = value.ToString();
			writer.WritePropertyName(stringValue ?? string.Empty);
		}
#endif
	}
}
