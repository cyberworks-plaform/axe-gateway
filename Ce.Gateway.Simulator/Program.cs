using Ce.Gateway.Simulator;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Ce.Gateway Request Simulator ===");
Console.WriteLine();

var dbPath = Path.Combine("..", "Ce.Gateway.Api", "data", "gateway.db");
var fullDbPath = Path.GetFullPath(dbPath);

if (!File.Exists(fullDbPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Database not found at: {fullDbPath}");
    Console.WriteLine("Please run the gateway application first to create the database.");
    Console.ResetColor();
    return;
}

Console.WriteLine($"📦 Database: {fullDbPath}");
Console.WriteLine();

var options = new DbContextOptionsBuilder<GatewayDbContext>()
    .UseSqlite($"Data Source={fullDbPath}")
    .Options;

var generator = new RequestLogGenerator();

Console.WriteLine("⚙️  Configuration:");
Console.WriteLine("   - Press ENTER for default (1 request every 2 seconds)");
Console.WriteLine("   - Or enter custom: <count> <interval_ms>");
Console.WriteLine("   - Example: 5 1000 (5 requests every 1 second)");
Console.Write("\n👉 Your choice: ");

var input = Console.ReadLine()?.Trim();
int requestsPerBatch = 1;
int intervalMs = 2000;

if (!string.IsNullOrEmpty(input))
{
    var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length >= 1 && int.TryParse(parts[0], out var count))
    {
        requestsPerBatch = count;
    }
    if (parts.Length >= 2 && int.TryParse(parts[1], out var interval))
    {
        intervalMs = interval;
    }
}

Console.WriteLine();
Console.WriteLine($"✅ Starting simulator:");
Console.WriteLine($"   - {requestsPerBatch} request(s) per batch");
Console.WriteLine($"   - Every {intervalMs}ms");
Console.WriteLine($"   - Press Ctrl+C to stop");
Console.WriteLine();

var totalGenerated = 0;
var startTime = DateTime.UtcNow;

try
{
    while (true)
    {
        var logs = generator.Generate(requestsPerBatch);

        using (var context = new GatewayDbContext(options))
        {
            context.OcrGatewayLogEntries.AddRange(logs);
            await context.SaveChangesAsync();
        }

        totalGenerated += requestsPerBatch;
        var elapsed = DateTime.UtcNow - startTime;
        var rate = totalGenerated / elapsed.TotalSeconds;

        Console.Write($"\r✨ Generated: {totalGenerated} | Rate: {rate:F2} req/s | Elapsed: {elapsed:hh\\:mm\\:ss}");

        await Task.Delay(intervalMs);
    }
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.ResetColor();
}

Console.WriteLine();
Console.WriteLine($"\n🏁 Simulator stopped. Total generated: {totalGenerated}");
