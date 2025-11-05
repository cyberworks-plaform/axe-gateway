using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories;
using Ce.Gateway.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    /// <summary>
    /// Performance tests using physical SQLite database file to measure real-world I/O performance
    /// Tests disk-based storage performance vs in-memory for production scenarios
    /// </summary>
    public class RequestReportPhysicalDbPerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _dbPath;
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public RequestReportPhysicalDbPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Create a physical SQLite database file in temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), "axe-gateway-tests");
            Directory.CreateDirectory(tempDir);
            _dbPath = Path.Combine(tempDir, $"performance_test_{Guid.NewGuid()}.db");
            
            _output.WriteLine($"Using physical database: {_dbPath}");
            
            var options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite($"Data Source={_dbPath}")
                .Options;

            _dbContextFactory = new TestDbContextFactory(options);

            // Create the schema
            using (var context = _dbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            
            // Setup configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RequestReport:CacheDefaultTtlMinutes", "30" },
                { "RequestReport:CacheShortTtlMinutes", "2" }
            });
            _configuration = configBuilder.Build();
        }

        /// <summary>
        /// Generates test data: 100K requests per day for specified number of days
        /// </summary>
        private async Task<long> GenerateTestDataAsync(int days, int requestsPerDay = 100000)
        {
            _output.WriteLine($"Generating {days} days of data with {requestsPerDay:N0} requests/day to physical file...");
            var sw = Stopwatch.StartNew();
            
            using var context = _dbContextFactory.CreateDbContext();
            var random = new Random(42); // Fixed seed for reproducibility
            var baseTime = DateTime.UtcNow.Date.AddDays(-days);
            
            var statusDistribution = new[] 
            { 
                (200, 70), // 70% success
                (404, 15), // 15% client error
                (500, 10), // 10% server error
                (301, 5)   // 5% other
            };

            long totalGenerated = 0;
            const int batchSize = 10000; // Insert in batches for performance

            for (int day = 0; day < days; day++)
            {
                var dayStart = baseTime.AddDays(day);
                var dayRequests = new List<RequestLogEntry>(batchSize);

                for (int i = 0; i < requestsPerDay; i++)
                {
                    // Distribute requests throughout the day
                    var timestamp = dayStart.AddSeconds(random.Next(0, 86400));
                    
                    // Select status code based on distribution
                    var statusRoll = random.Next(100);
                    int statusCode = 200;
                    var cumulative = 0;
                    foreach (var (code, percentage) in statusDistribution)
                    {
                        cumulative += percentage;
                        if (statusRoll < cumulative)
                        {
                            statusCode = code;
                            break;
                        }
                    }

                    dayRequests.Add(new RequestLogEntry
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = timestamp,
                        DownstreamStatusCode = statusCode,
                        UpstreamPath = $"/api/test{random.Next(1, 6)}",
                        DownstreamHost = $"node-{random.Next(1, 4)}",
                        UpstreamClientIp = $"192.168.1.{random.Next(1, 255)}",
                        TraceId = Guid.NewGuid().ToString(),
                        UpstreamHost = "gateway",
                        UpstreamScheme = "http",
                        UpstreamHttpMethod = "POST",
                        UpstreamQueryString = "",
                        DownstreamScheme = "http",
                        DownstreamPath = "/process",
                        DownstreamQueryString = "",
                        ErrorMessage = statusCode >= 400 ? "Error" : null,
                        RequestBody = null
                    });

                    // Insert in batches
                    if (dayRequests.Count >= batchSize)
                    {
                        context.OcrGatewayLogEntries.AddRange(dayRequests);
                        await context.SaveChangesAsync();
                        totalGenerated += dayRequests.Count;
                        dayRequests.Clear();

                        if (totalGenerated % 100000 == 0)
                        {
                            _output.WriteLine($"  Generated {totalGenerated:N0} requests...");
                        }
                    }
                }

                // Insert remaining requests for the day
                if (dayRequests.Count > 0)
                {
                    context.OcrGatewayLogEntries.AddRange(dayRequests);
                    await context.SaveChangesAsync();
                    totalGenerated += dayRequests.Count;
                }

                if ((day + 1) % 7 == 0)
                {
                    _output.WriteLine($"  Completed {day + 1} days ({totalGenerated:N0} total requests)");
                }
            }

            sw.Stop();
            
            // Get file size
            var fileInfo = new FileInfo(_dbPath);
            var fileSizeMB = fileInfo.Length / 1024.0 / 1024.0;
            
            _output.WriteLine($"Data generation complete: {totalGenerated:N0} requests in {sw.ElapsedMilliseconds:N0}ms ({totalGenerated / sw.Elapsed.TotalSeconds:N0} req/s)");
            _output.WriteLine($"Physical database size: {fileSizeMB:N2} MB");
            
            return totalGenerated;
        }

        [Fact]
        public async Task PhysicalDb_Performance_7Days_100KPerDay()
        {
            // Arrange - 7 days, 100K per day = 700K requests
            var requestsPerDay = 100000;
            var days = 7;
            await GenerateTestDataAsync(days, requestsPerDay);

            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            _output.WriteLine("\n=== PHYSICAL DB PERFORMANCE TEST: 7 Days (700K requests) ===");

            // Act - First call (uncached) - measures disk I/O
            var sw1 = Stopwatch.StartNew();
            var memBefore1 = GC.GetTotalMemory(true);
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            var memAfter1 = GC.GetTotalMemory(false);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (No Cache, Physical DB):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Memory Delta: {(memAfter1 - memBefore1) / 1024.0:N2} KB");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");
            _output.WriteLine($"  Disk I/O: Physical SQLite file read");

            // Act - Second call (cached)
            var sw2 = Stopwatch.StartNew();
            var memBefore2 = GC.GetTotalMemory(false);
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);
            var memAfter2 = GC.GetTotalMemory(false);
            sw2.Stop();

            _output.WriteLine($"\n2nd Query (Cached):");
            _output.WriteLine($"  Time: {sw2.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Memory Delta: {(memAfter2 - memBefore2) / 1024.0:N2} KB");
            _output.WriteLine($"  Cache Hit: {ReferenceEquals(result1, result2)}");

            // Third call to measure consistent cache performance
            var sw3 = Stopwatch.StartNew();
            var result3 = await service.GetReportAsync(from, to, Granularity.Day);
            sw3.Stop();

            _output.WriteLine($"\n3rd Query (Cached):");
            _output.WriteLine($"  Time: {sw3.ElapsedMilliseconds}ms");

            // Calculate improvements
            var avgCachedTime = (sw2.ElapsedMilliseconds + sw3.ElapsedMilliseconds) / 2.0;
            var timeImprovement = (1 - avgCachedTime / sw1.ElapsedMilliseconds) * 100;
            var speedup = sw1.ElapsedMilliseconds / avgCachedTime;

            _output.WriteLine($"\n=== PHYSICAL DB PERFORMANCE IMPROVEMENT ===");
            _output.WriteLine($"  Disk-based query time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Cached query time (avg): {avgCachedTime:F1}ms");
            _output.WriteLine($"  Time Saved: {timeImprovement:F1}% ({sw1.ElapsedMilliseconds - avgCachedTime:F1}ms faster)");
            _output.WriteLine($"  Speedup: {speedup:F1}x faster");
            _output.WriteLine($"  Cache Memory: ~{(memAfter1 - memBefore1) / 1024.0:N2} KB");
            
            // Get file size info
            var fileInfo = new FileInfo(_dbPath);
            _output.WriteLine($"  Database file size: {fileInfo.Length / 1024.0 / 1024.0:N2} MB");

            // Assert
            Assert.True(avgCachedTime <= sw1.ElapsedMilliseconds, "Cached queries should be equal or faster");
            Assert.Same(result1, result2);
            Assert.Same(result2, result3);
        }

        [Fact]
        public async Task PhysicalDb_WithAggregates_30Days_100KPerDay()
        {
            // Arrange - Create 30 days of aggregates (3M requests represented)
            var requestsPerDay = 100000;
            var days = 30;
            
            _output.WriteLine("\n=== PHYSICAL DB TEST: Aggregates (30 Days, 3M requests) ===");
            
            // Generate aggregates
            using (var context = _dbContextFactory.CreateDbContext())
            {
                var baseTime = DateTime.UtcNow.Date.AddDays(-days);
                var sw = Stopwatch.StartNew();

                for (int day = 0; day < days; day++)
                {
                    var periodStart = baseTime.AddDays(day);
                    
                    // Pre-aggregated data (70% success, 15% client error, 10% server error, 5% other)
                    context.RequestReportAggregates.Add(new RequestReportAggregate
                    {
                        PeriodStart = periodStart,
                        Granularity = "day",
                        StatusCategory = 2, // Success
                        Count = (long)(requestsPerDay * 0.70),
                        LastUpdatedAt = DateTime.UtcNow
                    });
                    
                    context.RequestReportAggregates.Add(new RequestReportAggregate
                    {
                        PeriodStart = periodStart,
                        Granularity = "day",
                        StatusCategory = 4, // Client error
                        Count = (long)(requestsPerDay * 0.15),
                        LastUpdatedAt = DateTime.UtcNow
                    });
                    
                    context.RequestReportAggregates.Add(new RequestReportAggregate
                    {
                        PeriodStart = periodStart,
                        Granularity = "day",
                        StatusCategory = 5, // Server error
                        Count = (long)(requestsPerDay * 0.10),
                        LastUpdatedAt = DateTime.UtcNow
                    });
                    
                    context.RequestReportAggregates.Add(new RequestReportAggregate
                    {
                        PeriodStart = periodStart,
                        Granularity = "day",
                        StatusCategory = 0, // Other
                        Count = (long)(requestsPerDay * 0.05),
                        LastUpdatedAt = DateTime.UtcNow
                    });
                }
                
                await context.SaveChangesAsync();
                sw.Stop();
                _output.WriteLine($"Aggregates created in {sw.ElapsedMilliseconds}ms (physical DB)");
            }

            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            // Query using aggregates from physical DB
            var sw1 = Stopwatch.StartNew();
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (Aggregates, Physical DB):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");

            // Cached query
            var sw2 = Stopwatch.StartNew();
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);
            sw2.Stop();

            _output.WriteLine($"\n2nd Query (Cached):");
            _output.WriteLine($"  Time: {sw2.ElapsedMilliseconds}ms");

            // Multiple cached queries
            var cachedTimes = new List<long>();
            for (int i = 0; i < 3; i++)
            {
                var swCached = Stopwatch.StartNew();
                await service.GetReportAsync(from, to, Granularity.Day);
                swCached.Stop();
                cachedTimes.Add(swCached.ElapsedMilliseconds);
            }

            var avgCachedTime = cachedTimes.Average();
            var improvement = (1 - avgCachedTime / sw1.ElapsedMilliseconds) * 100;
            
            _output.WriteLine($"\n=== PHYSICAL DB AGGREGATE + CACHE PERFORMANCE ===");
            _output.WriteLine($"  Physical DB aggregate query: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Cached query (avg): {avgCachedTime:F1}ms");
            _output.WriteLine($"  Cache improvement: {improvement:F1}%");
            _output.WriteLine($"  Data volume: 3M requests represented by 120 aggregate rows");
            _output.WriteLine($"  Compression ratio: {3000000.0 / 120:N0}:1");
            
            // Get file size
            var fileInfo = new FileInfo(_dbPath);
            _output.WriteLine($"  Database file size: {fileInfo.Length / 1024.0 / 1024.0:N2} MB");

            Assert.True(sw1.ElapsedMilliseconds < 1000, "Aggregate query should be fast (< 1000ms)");
            Assert.True(avgCachedTime <= sw1.ElapsedMilliseconds);
        }

        [Fact]
        public async Task PhysicalDb_CompareMemoryVsDisk_Performance()
        {
            // This test compares in-memory vs physical DB performance characteristics
            _output.WriteLine("\n=== COMPARING PHYSICAL DB vs IN-MEMORY PERFORMANCE ===");
            _output.WriteLine("Physical DB characteristics:");
            _output.WriteLine("  + Persistent data across runs");
            _output.WriteLine("  + Real-world I/O patterns");
            _output.WriteLine("  - Slower due to disk access");
            _output.WriteLine("  - File system overhead");
            
            var days = 3;
            var requestsPerDay = 10000; // Smaller dataset for quick comparison
            
            await GenerateTestDataAsync(days, requestsPerDay);
            
            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            // Measure first query (disk I/O)
            var sw1 = Stopwatch.StartNew();
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            sw1.Stop();

            // Measure cached query (memory)
            var sw2 = Stopwatch.StartNew();
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);
            sw2.Stop();

            _output.WriteLine($"\nResults:");
            _output.WriteLine($"  Physical DB query: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Cached (memory): {sw2.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Improvement: {(1 - (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds) * 100:F1}%");
            
            _output.WriteLine($"\nConclusion:");
            _output.WriteLine($"  Cache provides significant benefit for physical DB");
            _output.WriteLine($"  First query includes disk I/O overhead");
            _output.WriteLine($"  Subsequent queries served from memory (0ms typically)");
            
            Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds);
        }

        public void Dispose()
        {
            _memoryCache?.Dispose();
            
            // Clean up physical database file
            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                    _output.WriteLine($"\nCleaned up test database: {_dbPath}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"\nWarning: Could not delete test database: {ex.Message}");
                }
            }
        }

        private class TestDbContextFactory : IDbContextFactory<GatewayDbContext>
        {
            private readonly DbContextOptions<GatewayDbContext> _options;

            public TestDbContextFactory(DbContextOptions<GatewayDbContext> options)
            {
                _options = options;
            }

            public GatewayDbContext CreateDbContext()
            {
                return new GatewayDbContext(_options);
            }
        }
    }
}
