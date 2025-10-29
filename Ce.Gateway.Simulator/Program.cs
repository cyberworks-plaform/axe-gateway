using Ce.Gateway.Simulator;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Ce.Gateway Request Simulator ===");
Console.WriteLine();

var dbPath = Path.Combine("..", "Ce.Gateway.Api", "data", "gateway.db");
var fullDbPath = Path.GetFullPath(dbPath);

if (!File.Exists(fullDbPath))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[ERROR] Database not found at: " + fullDbPath);
    Console.WriteLine("Please run the gateway application first to create the database.");
    Console.ResetColor();
    return;
}

Console.WriteLine("[OK] Database: " + fullDbPath);
Console.WriteLine();

var options = new DbContextOptionsBuilder<GatewayDbContext>()
    .UseSqlite($"Data Source={fullDbPath}")
    .Options;

// Menu selection
Console.WriteLine("Select Mode:");
Console.WriteLine("  1 - Real-time mode (continuous generation)");
Console.WriteLine("  2 - Historical data mode (generate past data)");
Console.Write("\nYour choice [1/2]: ");

var modeChoice = Console.ReadLine()?.Trim();

if (modeChoice == "2")
{
    await GenerateHistoricalDataAsync(options);
}
else
{
    await RunRealTimeModeAsync(options);
}

async Task RunRealTimeModeAsync(DbContextOptions<GatewayDbContext> dbOptions)
{
    var generator = new RequestLogGenerator();

    Console.WriteLine();
    Console.WriteLine("Configuration:");
    Console.WriteLine("   - Press ENTER for default (1 request every 2 seconds)");
    Console.WriteLine("   - Or enter custom: <count> <interval_ms>");
    Console.WriteLine("   - Example: 5 1000 (5 requests every 1 second)");
    Console.Write("\nYour choice: ");

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
    Console.WriteLine("[START] Real-time mode:");
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

            using (var context = new GatewayDbContext(dbOptions))
            {
                context.OcrGatewayLogEntries.AddRange(logs);
                await context.SaveChangesAsync();
            }

            totalGenerated += requestsPerBatch;
            var elapsed = DateTime.UtcNow - startTime;
            var rate = totalGenerated / elapsed.TotalSeconds;

            Console.Write($"\rGenerated: {totalGenerated} | Rate: {rate:F2} req/s | Elapsed: {elapsed:hh\\:mm\\:ss}");

            await Task.Delay(intervalMs);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n[ERROR] {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
    Console.WriteLine($"\n[DONE] Total generated: {totalGenerated}");
}

async Task GenerateHistoricalDataAsync(DbContextOptions<GatewayDbContext> dbOptions)
{
    Console.WriteLine();
    Console.WriteLine("Historical Data Generator");
    Console.WriteLine("-------------------------");
    Console.WriteLine("Select time range:");
    Console.WriteLine("  1 - Last 24 hours (hourly distribution)");
    Console.WriteLine("  2 - Last 7 days (daily distribution)");
    Console.WriteLine("  3 - Last 30 days (daily distribution)");
    Console.WriteLine("  4 - Last 90 days (daily distribution)");
    Console.WriteLine("  5 - Last 12 months (monthly distribution)");
    Console.Write("\nYour choice [1-5]: ");

    var rangeChoice = Console.ReadLine()?.Trim();
    
    DateTime startDate;
    DateTime endDate = DateTime.UtcNow;
    string timeUnit;
    int requestsPerSlot;

    switch (rangeChoice)
    {
        case "1":
            startDate = endDate.AddHours(-24);
            timeUnit = "hour";
            requestsPerSlot = 50;
            break;
        case "2":
            startDate = endDate.AddDays(-7);
            timeUnit = "day";
            requestsPerSlot = 200;
            break;
        case "3":
            startDate = endDate.AddDays(-30);
            timeUnit = "day";
            requestsPerSlot = 150;
            break;
        case "4":
            startDate = endDate.AddDays(-90);
            timeUnit = "day";
            requestsPerSlot = 100;
            break;
        case "5":
            startDate = endDate.AddMonths(-12);
            timeUnit = "month";
            requestsPerSlot = 500;
            break;
        default:
            startDate = endDate.AddHours(-24);
            timeUnit = "hour";
            requestsPerSlot = 50;
            break;
    }

    Console.Write($"\nRequests per {timeUnit} (default: {requestsPerSlot}): ");
    var reqInput = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(reqInput) && int.TryParse(reqInput, out var customReq))
    {
        requestsPerSlot = customReq;
    }

    Console.WriteLine();
    Console.WriteLine("[INFO] Generating historical data...");
    Console.WriteLine($"   - From: {startDate:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"   - To: {endDate:yyyy-MM-dd HH:mm}");
    Console.WriteLine($"   - Distribution: {requestsPerSlot} requests per {timeUnit}");
    Console.WriteLine();

    var generator = new RequestLogGenerator();
    var totalGenerated = 0;
    var random = new Random();

    var timeSlots = new List<DateTime>();
    
    if (timeUnit == "hour")
    {
        for (var dt = startDate; dt <= endDate; dt = dt.AddHours(1))
        {
            timeSlots.Add(dt);
        }
    }
    else if (timeUnit == "month")
    {
        for (var dt = startDate; dt <= endDate; dt = dt.AddMonths(1))
        {
            timeSlots.Add(dt);
        }
    }
    else // day
    {
        for (var dt = startDate.Date; dt <= endDate.Date; dt = dt.AddDays(1))
        {
            timeSlots.Add(dt);
        }
    }

    Console.WriteLine($"[INFO] Total time slots: {timeSlots.Count}");
    Console.WriteLine();

    foreach (var slot in timeSlots)
    {
        var variance = (int)(requestsPerSlot * 0.3);
        var actualCount = requestsPerSlot + random.Next(-variance, variance + 1);
        if (actualCount < 1) actualCount = 1;

        var logs = new List<RequestLogEntry>();

        for (int i = 0; i < actualCount; i++)
        {
            var log = generator.Generate();
            
            // Override CreatedAtUtc to be within the time slot
            if (timeUnit == "hour")
            {
                log.CreatedAtUtc = slot.AddMinutes(random.Next(0, 60))
                    .AddSeconds(random.Next(0, 60));
            }
            else if (timeUnit == "month")
            {
                var daysInMonth = DateTime.DaysInMonth(slot.Year, slot.Month);
                log.CreatedAtUtc = slot.AddDays(random.Next(0, daysInMonth))
                    .AddHours(random.Next(0, 24))
                    .AddMinutes(random.Next(0, 60));
            }
            else // day
            {
                log.CreatedAtUtc = slot.AddHours(random.Next(0, 24))
                    .AddMinutes(random.Next(0, 60))
                    .AddSeconds(random.Next(0, 60));
            }

            logs.Add(log);
        }

        using (var context = new GatewayDbContext(dbOptions))
        {
            context.OcrGatewayLogEntries.AddRange(logs);
            await context.SaveChangesAsync();
        }

        totalGenerated += logs.Count;
        var progress = (timeSlots.IndexOf(slot) + 1) * 100 / timeSlots.Count;
        Console.Write($"\rProgress: {progress}% | Generated: {totalGenerated} requests");
    }

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"[DONE] Historical data generated successfully!");
    Console.WriteLine($"   - Total requests: {totalGenerated}");
    Console.WriteLine($"   - Time range: {startDate:yyyy-MM-dd HH:mm} to {endDate:yyyy-MM-dd HH:mm}");
}

