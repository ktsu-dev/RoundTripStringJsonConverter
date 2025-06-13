using System.Text.Json;

// Test types from the integration tests
public class ProductCode(string code)
{
    public string Code { get; } = code;
    public static ProductCode Parse(string code) => new(code);
    public override string ToString() => Code;
    public override bool Equals(object? obj) => obj is ProductCode other && Code == other.Code;
    public override int GetHashCode() => Code.GetHashCode();
}

public class Order
{
    public IList<ProductCode> Products { get; } = [];
}

class Program
{
    static void Main()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        var original = new Order();
        original.Products.Add(ProductCode.Parse("PROD-A"));
        original.Products.Add(ProductCode.Parse("PROD-B"));
        original.Products.Add(ProductCode.Parse("PROD-C"));

        Console.WriteLine($"Original count: {original.Products.Count}");

        string json = JsonSerializer.Serialize(original, options);
        Console.WriteLine("JSON:");
        Console.WriteLine(json);

        Order? deserialized = JsonSerializer.Deserialize<Order>(json, options);
        Console.WriteLine($"Deserialized count: {deserialized?.Products.Count ?? -1}");

        if (deserialized != null && deserialized.Products.Count > 0)
        {
            Console.WriteLine($"First item type: {deserialized.Products[0].GetType()}");
            Console.WriteLine($"First item value: {deserialized.Products[0]}");
        }
    }
}
