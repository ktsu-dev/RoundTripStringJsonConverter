// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.RoundTripStringJsonConverter;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

		// Leave System.Text.Json's own types (JsonNode, JsonDocument, JsonElement, ...) to its built-in
		// converters. They have a usable Parse(string), but they serialize as JSON, not as a string.
		if (type.Namespace is not null &&
			(type.Namespace == "System.Text.Json" || type.Namespace.StartsWith("System.Text.Json.", StringComparison.Ordinal)))
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

				// Prefer an exact single-string overload. Otherwise take the first overload whose extra
				// parameters can all be supplied, so GetMethods() ordering can't pick one we can't call.
				MethodInfo? match = methods.FirstOrDefault(m => m.GetParameters().Length == 1 && IsUsableConversionMethod(m, type))
					?? methods.FirstOrDefault(m => IsUsableConversionMethod(m, type));
				if (match is not null)
				{
					return match;
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
	/// Determines whether a method can convert a string to the specified type: its first parameter is a
	/// string, it returns the type, and every further parameter is an <see cref="IFormatProvider"/> or optional.
	/// </summary>
	/// <param name="method">The candidate method.</param>
	/// <param name="type">The type to convert to.</param>
	/// <returns>True if the method can be invoked by <see cref="BuildArguments"/>; otherwise, false.</returns>
	private static bool IsUsableConversionMethod(MethodInfo method, Type type)
	{
		ParameterInfo[] parameters = method.GetParameters();
		return parameters.Length > 0 &&
			parameters[0].ParameterType == typeof(string) &&
			parameters.Skip(1).All(p => p.ParameterType == typeof(IFormatProvider) || p.IsOptional) &&
			(method.ReturnType == type ||
			 (method.ReturnType.IsGenericType && type.IsGenericType &&
			  method.ReturnType.GetGenericTypeDefinition() == type.GetGenericTypeDefinition()));
	}

	/// <summary>
	/// Builds the argument list for a conversion method accepted by <see cref="IsUsableConversionMethod"/>.
	/// An <see cref="IFormatProvider"/> gets the invariant culture and an optional parameter gets its default.
	/// </summary>
	/// <param name="method">The conversion method.</param>
	/// <param name="value">The string to convert.</param>
	/// <returns>The arguments to invoke the method with.</returns>
	private static object?[] BuildArguments(MethodInfo method, string value)
	{
		ParameterInfo[] parameters = method.GetParameters();
		object?[] arguments = new object?[parameters.Length];
		arguments[0] = value;
		for (int i = 1; i < parameters.Length; i++)
		{
			arguments[i] = parameters[i].ParameterType == typeof(IFormatProvider)
				? CultureInfo.InvariantCulture
				: Type.Missing;
		}

		return arguments;
	}

	/// <summary>
	/// Determines whether the specified type can be converted by this factory.
	/// </summary>
	/// <param name="typeToConvert">The type to check for conversion capability.</param>
	/// <returns>True if the type can be converted; otherwise, false.</returns>
	public override bool CanConvert(Type typeToConvert)
	{
		Ensure.NotNull(typeToConvert, nameof(typeToConvert));

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
		Ensure.NotNull(typeToConvert, nameof(typeToConvert));
		Ensure.NotNull(options, nameof(options));
		Type converterType = typeof(RoundTripStringJsonConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter)Activator.CreateInstance(converterType, BindingFlags.Instance | BindingFlags.Public, binder: null, args: null, culture: null)!;
	}

	/// <summary>
	/// JSON converter that uses a type's ToString and string conversion methods for serialization.
	/// Supports various string conversion methods including FromString, Parse, Create, and Convert.
	/// </summary>
	/// <typeparam name="T">The type to be converted.</typeparam>
	[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated via reflection in CreateConverter using Activator.CreateInstance")]
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
			Ensure.NotNull(typeToConvert, nameof(typeToConvert));

			if (reader.TokenType == JsonTokenType.Null)
			{
				return default;
			}

			if (reader.TokenType != JsonTokenType.String)
			{
				throw new JsonException($"Expected string token, got {reader.TokenType}");
			}

			string? stringValue = reader.GetString();
			Ensure.NotNull(stringValue, nameof(stringValue));

			try
			{
				return (T)StringConversionMethod!.Invoke(null, BuildArguments(StringConversionMethod, stringValue))!;
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
			Ensure.NotNull(typeToConvert, nameof(typeToConvert));

			string? stringValue = reader.GetString();
			Ensure.NotNull(stringValue, nameof(stringValue));

			try
			{
				return (T)StringConversionMethod!.Invoke(null, BuildArguments(StringConversionMethod, stringValue))!;
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
			Ensure.NotNull(writer, nameof(writer));
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
			Ensure.NotNull(writer, nameof(writer));

#pragma warning disable KTSU0004 // Use Ensure.NotNull instead of manual null check
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}
#pragma warning restore KTSU0004 // Use Ensure.NotNull instead of manual null check

			string? stringValue = value.ToString();
			writer.WritePropertyName(stringValue ?? string.Empty);
		}
#endif
	}
}
