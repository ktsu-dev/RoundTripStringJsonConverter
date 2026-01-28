# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

- `dotnet build` - Build the solution
- `dotnet test` - Run all tests
- `dotnet test --filter "FullyQualifiedName~TestName"` - Run specific test

## Architecture

This is a single-purpose .NET library that provides a `System.Text.Json` converter factory for round-trip serialization of types via their string representation.

### Core Component

**RoundTripStringJsonConverterFactory** (`RoundTripStringJsonConverter/RoundTripStringJsonConverter.cs`)
- A `JsonConverterFactory` that serializes objects using `ToString()` and deserializes using static string conversion methods
- Supports conversion methods in priority order: `FromString`, `Parse`, `Create`, `Convert`
- Contains a nested generic `RoundTripStringJsonConverter<T>` that performs the actual conversion
- Uses cached reflection for performance

### Type Compatibility Requirements

For a type to work with this converter, it must have:
1. A public static method named `FromString`, `Parse`, `Create`, or `Convert` that takes a single `string` parameter and returns the type
2. A `ToString()` override that produces a string reversible by the conversion method

### Multi-Targeting

The library targets `netstandard2.0`, `netstandard2.1`, and .NET 5-10. Uses `#if NET6_0_OR_GREATER` for `ReadAsPropertyName`/`WriteAsPropertyName` methods which enable dictionary key support in newer frameworks.

### Project Structure

- `RoundTripStringJsonConverter/` - Main library (multi-targets netstandard2.0/2.1 and net5.0+)
- `RoundTripStringJsonConverter.Test/` - MSTest project (targets latest .NET only)
- `DebugTest/` - Ad-hoc console test project (not in solution)

### SDK

Uses `ktsu.Sdk` which provides common build configuration. The SDK handles target frameworks, packaging, and other shared settings.
