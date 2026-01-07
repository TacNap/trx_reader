using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

const string DefaultImagePath = "input.png";

var imagePath = args.Length > 0 ? args[0] : DefaultImagePath;

Console.WriteLine($"Parsing receipt from: {imagePath}");
Console.WriteLine(new string('-', 40));

var receipt = await ParseReceiptAsync(imagePath);

Console.WriteLine($"Store: {receipt.StoreName ?? "Unknown"}");
Console.WriteLine($"Date: {receipt.Date ?? "Unknown"}");
Console.WriteLine($"Items: {receipt.Items.Count}");
Console.WriteLine($"Total: ${receipt.CalculatedTotal:F2}");
Console.WriteLine();
Console.WriteLine("Items:");
foreach (var item in receipt.Items)
{
    var weight = item.Weight != null ? $" ({item.Weight})" : "";
    Console.WriteLine($"  - {item.Name}{weight}: {item.Quantity} x ${item.Price:F2}");
}

static async Task<Receipt> ParseReceiptAsync(string imagePath)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"receipt_parser.py \"{imagePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }
    };

    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    var error = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new Exception($"Python script failed: {error}");
    }

    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    return JsonSerializer.Deserialize<Receipt>(output, options)
        ?? throw new Exception("Failed to parse receipt JSON");
}

record Receipt(
    [property: JsonPropertyName("store_name")] string? StoreName,
    [property: JsonPropertyName("date")] string? Date,
    [property: JsonPropertyName("items")] List<ReceiptItem> Items,
    [property: JsonPropertyName("calculated_total")] decimal CalculatedTotal
);

record ReceiptItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("weight")] string? Weight
);
