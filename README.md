# ktsu.RoundTripStringJsonConverter

> A versatile JSON converter factory that serializes objects using their ToString method and deserializes using FromString, Parse, Create, or Convert methods.

[![License](https://img.shields.io/github/license/ktsu-dev/RoundTripStringJsonConverter)](https://github.com/ktsu-dev/RoundTripStringJsonConverter/blob/main/LICENSE.md)
[![NuGet](https://img.shields.io/nuget/v/ktsu.RoundTripStringJsonConverter.svg)](https://www.nuget.org/packages/ktsu.RoundTripStringJsonConverter/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.RoundTripStringJsonConverter.svg)](https://www.nuget.org/packages/ktsu.RoundTripStringJsonConverter/)
[![Build Status](https://github.com/ktsu-dev/RoundTripStringJsonConverter/workflows/build/badge.svg)](https://github.com/ktsu-dev/RoundTripStringJsonConverter/actions)
[![GitHub Stars](https://img.shields.io/github/stars/ktsu-dev/RoundTripStringJsonConverter?style=social)](https://github.com/ktsu-dev/RoundTripStringJsonConverter/stargazers)

## Introduction

`RoundTripStringJsonConverter` is a powerful JSON converter factory for System.Text.Json that simplifies serialization and deserialization of custom types by leveraging their string representation methods. It automatically detects and uses the most appropriate conversion method from a prioritized list: `FromString`, `Parse`, `Create`, or `Convert`. This approach is particularly useful for value types, strong types, domain objects, and any other types where a string representation makes logical sense.

## Features

- **Multiple Conversion Methods**: Supports `FromString`, `Parse`, `Create`, and `Convert` methods with intelligent priority selection
- **Automatic Type Detection**: Automatically identifies types with compatible conversion methods
- **Method Priority System**: Uses `FromString` first, then `Parse`, `Create`, and finally `Convert`
- **String-Based Serialization**: Converts objects to and from JSON using their string representation
- **Property Name Support**: Works with both JSON values and property names (dictionary keys)
- **Reflection Optimization**: Uses cached reflection for improved performance
- **Generic Method Support**: Handles both generic and non-generic conversion methods
- **Comprehensive Error Handling**: Graceful handling of invalid types and conversion failures

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.RoundTripStringJsonConverter
```

### .NET CLI

```bash
dotnet add package ktsu.RoundTripStringJsonConverter
```

### Package Reference

```xml
<PackageReference Include="ktsu.RoundTripStringJsonConverter" Version="x.y.z" />
```

## Usage Examples

### Basic Example with FromString

```csharp
using System.Text.Json;
using ktsu.RoundTripStringJsonConverter;

// Configure the converter in your JsonSerializerOptions
var options = new JsonSerializerOptions();
options.Converters.Add(new RoundTripStringJsonConverterFactory());

// Example custom type with ToString and FromString
public class UserId
{
    public string Value { get; set; }
    
    public static UserId FromString(string value) => new() { Value = value };
    
    public override string ToString() => Value;
}

// Serialization
var userId = new UserId { Value = "USER-12345" };
string json = JsonSerializer.Serialize(userId, options);
// json is now: "USER-12345"

// Deserialization
UserId deserialized = JsonSerializer.Deserialize<UserId>(json, options);
// deserialized.Value is now: "USER-12345"
```

### Using Different Conversion Methods

The converter automatically detects and uses the appropriate method based on priority:

```csharp
// Type with Parse method (common in .NET)
public class ProductCode
{
    public string Code { get; set; }
    
    public static ProductCode Parse(string code) => new() { Code = code };
    
    public override string ToString() => Code;
}

// Type with Create method (factory pattern)
public class OrderId
{
    public string Id { get; set; }
    
    public static OrderId Create(string id) => new() { Id = id };
    
    public override string ToString() => Id;
}

// Type with Convert method
public class CategoryName
{
    public string Name { get; set; }
    
    public static CategoryName Convert(string name) => new() { Name = name };
    
    public override string ToString() => Name;
}

// All types work seamlessly with the same converter
var options = new JsonSerializerOptions();
options.Converters.Add(new RoundTripStringJsonConverterFactory());

var product = ProductCode.Parse("PROD-ABC");
var order = OrderId.Create("ORD-001");
var category = CategoryName.Convert("Electronics");

// All serialize and deserialize correctly
string productJson = JsonSerializer.Serialize(product, options);
string orderJson = JsonSerializer.Serialize(order, options);
string categoryJson = JsonSerializer.Serialize(category, options);
```

### Method Priority Example

When multiple methods are available, the converter uses this priority order:

```csharp
public class MultiMethodType
{
    public string Value { get; set; }
    
    // Highest priority - will be used
    public static MultiMethodType FromString(string value) => new() { Value = $"FromString:{value}" };
    
    // Lower priority - will be ignored
    public static MultiMethodType Parse(string value) => new() { Value = $"Parse:{value}" };
    
    public override string ToString() => Value;
}

// FromString method will be used for deserialization
var result = JsonSerializer.Deserialize<MultiMethodType>("\"test\"", options);
// result.Value will be "FromString:test"
```

### Integration with Other Converters

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using ktsu.RoundTripStringJsonConverter;

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters =
    {
        new RoundTripStringJsonConverterFactory(),
        new JsonStringEnumConverter()
    }
};

// Now both enum values and custom types with conversion methods will be handled appropriately
```

## Advanced Usage

### Working with Collections of Custom Types

```csharp
using System.Text.Json;
using ktsu.RoundTripStringJsonConverter;

// Setup serializer options with the converter
var options = new JsonSerializerOptions();
options.Converters.Add(new RoundTripStringJsonConverterFactory());

// A collection of custom types
List<UserId> userIds = new()
{
    UserId.FromString("USER-001"),
    UserId.FromString("USER-002"),
    UserId.FromString("USER-003")
};

// Serialize the collection
string json = JsonSerializer.Serialize(userIds, options);
// json is now: ["USER-001","USER-002","USER-003"]

// Deserialize back to a collection
List<UserId> deserializedIds = JsonSerializer.Deserialize<List<UserId>>(json, options);
```

### Using with Dictionaries as Keys

```csharp
// Custom types can be used as dictionary keys
var userProducts = new Dictionary<UserId, List<ProductCode>>
{
    { UserId.FromString("USER-001"), [ProductCode.Parse("PROD-A"), ProductCode.Parse("PROD-B")] },
    { UserId.FromString("USER-002"), [ProductCode.Parse("PROD-C")] }
};

string json = JsonSerializer.Serialize(userProducts, options);
// Serializes as a dictionary with string keys

var deserialized = JsonSerializer.Deserialize<Dictionary<UserId, List<ProductCode>>>(json, options);
// Keys are properly deserialized back to UserId objects
```

### Complex Domain Objects

```csharp
public class Order
{
    public OrderId Id { get; set; }
    public UserId CustomerId { get; set; }
    public List<ProductCode> Products { get; set; }
    public Dictionary<CategoryName, int> CategoryCounts { get; set; }
    public DateTime OrderDate { get; set; }
}

// All custom types are automatically handled
var order = new Order
{
    Id = OrderId.Create("ORD-001"),
    CustomerId = UserId.FromString("USER-123"),
    Products = [ProductCode.Parse("PROD-A"), ProductCode.Parse("PROD-B")],
    CategoryCounts = new Dictionary<CategoryName, int>
    {
        { CategoryName.Convert("Electronics"), 2 }
    },
    OrderDate = DateTime.UtcNow
};

string json = JsonSerializer.Serialize(order, options);
Order deserializedOrder = JsonSerializer.Deserialize<Order>(json, options);
```

## API Reference

### RoundTripStringJsonConverterFactory

The primary class for integrating with System.Text.Json serialization.

#### Methods

| Name | Return Type | Description |
|------|-------------|-------------|
| `CanConvert(Type typeToConvert)` | `bool` | Determines if a type can be converted by checking for compatible conversion methods |
| `CreateConverter(Type typeToConvert, JsonSerializerOptions options)` | `JsonConverter` | Creates a type-specific converter instance |

### Supported Conversion Methods

The converter looks for static methods in this priority order:

1. **FromString(string)** - Highest priority, commonly used for custom types
2. **Parse(string)** - Second priority, follows .NET conventions (like int.Parse)
3. **Create(string)** - Third priority, factory pattern methods
4. **Convert(string)** - Lowest priority, general conversion methods

### Compatibility Requirements

For a type to work with RoundTripStringJsonConverter, it must meet these requirements:

1. Have a public static method with one of the supported names (`FromString`, `Parse`, `Create`, or `Convert`)
2. The method must take a single `string` parameter
3. The method must return an instance of the declaring type
4. Override `ToString()` to provide a string representation that can be reversed by the conversion method

#### Valid Method Signatures

```csharp
// All of these are valid conversion methods:
public static MyType FromString(string value) { ... }
public static MyType Parse(string value) { ... }
public static MyType Create(string value) { ... }
public static MyType Convert(string value) { ... }
```

#### Invalid Method Signatures

```csharp
// These will NOT work:
public MyType FromString(string value) { ... }        // Not static
public static MyType FromString(int value) { ... }    // Wrong parameter type
public static string FromString(string value) { ... } // Wrong return type
public static MyType FromString(string value, IFormatProvider provider) { ... } // Too many parameters
```

## Performance Considerations

- **Reflection Caching**: Method information is cached for improved performance on repeated conversions
- **Large Collections**: Tested with collections of 1000+ items
- **Memory Efficiency**: Minimal memory overhead per conversion
- **Thread Safety**: The converter factory is thread-safe

## Error Handling

The converter provides comprehensive error handling:

- **Invalid JSON Types**: Throws `JsonException` for non-string JSON tokens
- **Conversion Failures**: Propagates exceptions from conversion methods
- **Missing Methods**: Types without valid conversion methods are ignored
- **Null Arguments**: Proper `ArgumentNullException` handling

## Migration from ToStringJsonConverter

If you're migrating from the previous `ToStringJsonConverter`:

1. Update package reference to `ktsu.RoundTripStringJsonConverter`
2. Update using statements: `using ktsu.RoundTripStringJsonConverter;`
3. Update converter instantiation: `new RoundTripStringJsonConverterFactory()`
4. Your existing `FromString` methods will continue to work unchanged
5. You can now also use `Parse`, `Create`, or `Convert` methods

## Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please make sure to update tests as appropriate.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
