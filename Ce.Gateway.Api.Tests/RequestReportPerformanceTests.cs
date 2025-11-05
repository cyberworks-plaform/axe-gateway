using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories;
using Ce.Gateway.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    /// <summary>
    /// Performance tests with large-scale data to measure cache effectiveness
    /// Tests with 100K requests/day, 3M/month, 36M/year as requested
    /// </summary>
    public class RequestReportPerformanceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SqliteConnection _connection;
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;

        public RequestReportPerformanceTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Setup in-memory SQLite database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            
            var options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite(_connection)
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
            _output.WriteLine($"Generating {days} days of data with {requestsPerDay:N0} requests/day...");
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

                if ((day + 1) % 30 == 0)
                {
                    _output.WriteLine($"  Completed {day + 1} days ({totalGenerated:N0} total requests)");
                }
            }

            sw.Stop();
            _output.WriteLine($"Data generation complete: {totalGenerated:N0} requests in {sw.ElapsedMilliseconds:N0}ms ({totalGenerated / sw.Elapsed.TotalSeconds:N0} req/s)");
            _output.WriteLine($"Database size: ~{totalGenerated * 500 / 1024 / 1024:N2} MB (estimated)");
            
            return totalGenerated;
        }

        [Fact]
        public async Task Performance_SmallDataset_1Week_100KPerDay()
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

            _output.WriteLine("\n=== PERFORMANCE TEST: 7 Days (700K requests) ===");

            // Act - First call (uncached)
            var sw1 = Stopwatch.StartNew();
            var memBefore1 = GC.GetTotalMemory(true);
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            var memAfter1 = GC.GetTotalMemory(false);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (No Cache):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Memory Delta: {(memAfter1 - memBefore1) / 1024.0:N2} KB");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");

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

            // Calculate improvements
            var timeImprovement = (1 - (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds) * 100;
            var speedup = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;

            _output.WriteLine($"\n=== PERFORMANCE IMPROVEMENT ===");
            _output.WriteLine($"  Time Saved: {timeImprovement:F1}% ({sw1.ElapsedMilliseconds - sw2.ElapsedMilliseconds}ms faster)");
            _output.WriteLine($"  Speedup: {speedup:F1}x faster");
            _output.WriteLine($"  Cache Memory: ~{(memAfter1 - memBefore1) / 1024.0:N2} KB");

            // Assert
            Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds, "Cached query should be equal or faster");
            Assert.Same(result1, result2);
        }

        [Fact]
        public async Task Performance_MediumDataset_1Month_100KPerDay()
        {
            // Arrange - 30 days, 100K per day = 3M requests
            var requestsPerDay = 100000;
            var days = 30;
            await GenerateTestDataAsync(days, requestsPerDay);

            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            _output.WriteLine("\n=== PERFORMANCE TEST: 30 Days (3M requests) ===");

            // Act - First call (uncached)
            var sw1 = Stopwatch.StartNew();
            var memBefore1 = GC.GetTotalMemory(true);
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            var memAfter1 = GC.GetTotalMemory(false);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (No Cache):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Memory Delta: {(memAfter1 - memBefore1) / 1024.0:N2} KB");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");

            // Second call (cached)
            var sw2 = Stopwatch.StartNew();
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);
            sw2.Stop();

            _output.WriteLine($"\n2nd Query (Cached):");
            _output.WriteLine($"  Time: {sw2.ElapsedMilliseconds}ms");

            // Third call (still cached)
            var sw3 = Stopwatch.StartNew();
            var result3 = await service.GetReportAsync(from, to, Granularity.Day);
            sw3.Stop();

            _output.WriteLine($"\n3rd Query (Cached):");
            _output.WriteLine($"  Time: {sw3.ElapsedMilliseconds}ms");

            var avgCachedTime = (sw2.ElapsedMilliseconds + sw3.ElapsedMilliseconds) / 2.0;
            var timeImprovement = (1 - avgCachedTime / sw1.ElapsedMilliseconds) * 100;
            var speedup = sw1.ElapsedMilliseconds / avgCachedTime;

            _output.WriteLine($"\n=== PERFORMANCE IMPROVEMENT ===");
            _output.WriteLine($"  Time Saved: {timeImprovement:F1}% (avg {avgCachedTime:F1}ms vs {sw1.ElapsedMilliseconds}ms)");
            _output.WriteLine($"  Speedup: {speedup:F1}x faster");
            _output.WriteLine($"  Cache Memory: ~{(memAfter1 - memBefore1) / 1024.0:N2} KB");

            Assert.True(avgCachedTime <= sw1.ElapsedMilliseconds);
        }

        [Fact]
        public async Task Performance_LargeDataset_3Months_100KPerDay()
        {
            // Arrange - 90 days, 100K per day = 9M requests
            var requestsPerDay = 100000;
            var days = 90;
            await GenerateTestDataAsync(days, requestsPerDay);

            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            _output.WriteLine("\n=== PERFORMANCE TEST: 90 Days (9M requests) ===");

            // Measure uncached performance
            var sw1 = Stopwatch.StartNew();
            var memBefore = GC.GetTotalMemory(true);
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            var memAfter = GC.GetTotalMemory(false);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (No Cache):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms ({sw1.Elapsed.TotalSeconds:F2}s)");
            _output.WriteLine($"  Memory Delta: {(memAfter - memBefore) / 1024.0:N2} KB");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");
            _output.WriteLine($"  Query Rate: {result1.TotalRequests / sw1.Elapsed.TotalSeconds:N0} req/s");

            // Measure cached performance (multiple times)
            var cachedTimes = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                var swCached = Stopwatch.StartNew();
                await service.GetReportAsync(from, to, Granularity.Day);
                swCached.Stop();
                cachedTimes.Add(swCached.ElapsedMilliseconds);
            }

            var avgCachedTime = cachedTimes.Average();
            var minCachedTime = cachedTimes.Min();
            var maxCachedTime = cachedTimes.Max();

            _output.WriteLine($"\nCached Queries (5 iterations):");
            _output.WriteLine($"  Min: {minCachedTime}ms");
            _output.WriteLine($"  Max: {maxCachedTime}ms");
            _output.WriteLine($"  Avg: {avgCachedTime:F1}ms");

            var timeImprovement = (1 - avgCachedTime / sw1.ElapsedMilliseconds) * 100;
            var speedup = sw1.ElapsedMilliseconds / avgCachedTime;
            var timeSaved = sw1.ElapsedMilliseconds - avgCachedTime;

            _output.WriteLine($"\n=== PERFORMANCE IMPROVEMENT ===");
            _output.WriteLine($"  Time Saved: {timeImprovement:F1}%");
            _output.WriteLine($"  Absolute Saving: {timeSaved:F1}ms per query");
            _output.WriteLine($"  Speedup: {speedup:F1}x faster");
            _output.WriteLine($"  Cache Memory: ~{(memAfter - memBefore) / 1024.0:N2} KB per entry");
            
            // Calculate savings for typical usage
            _output.WriteLine($"\n=== TYPICAL USAGE SCENARIO ===");
            var queriesPerDay = 100; // Assume 100 report views per day
            var dailySavings = timeSaved * queriesPerDay / 1000.0; // Convert to seconds
            var monthlySavings = dailySavings * 30;
            _output.WriteLine($"  Queries per day: {queriesPerDay}");
            _output.WriteLine($"  Daily time saved: {dailySavings:F1}s");
            _output.WriteLine($"  Monthly time saved: {monthlySavings:F1}s ({monthlySavings / 60:F1} minutes)");

            Assert.True(avgCachedTime < sw1.ElapsedMilliseconds * 0.5, "Cache should provide at least 50% improvement");
        }

        [Fact]
        public async Task Performance_WithAggregates_3Months_100KPerDay()
        {
            // Arrange - Create 90 days of data (9M requests) but use aggregates
            var requestsPerDay = 100000;
            var days = 90;
            
            _output.WriteLine("\n=== PERFORMANCE TEST: Aggregates vs Raw (90 Days, 9M requests) ===");
            
            // Generate aggregates instead of raw data (much faster)
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
                _output.WriteLine($"Aggregates created in {sw.ElapsedMilliseconds}ms");
            }

            var repoLogger = new Mock<ILogger<RequestReportRepository>>();
            var serviceLogger = new Mock<ILogger<RequestReportService>>();
            var repository = new RequestReportRepository(_dbContextFactory, repoLogger.Object);
            var service = new RequestReportService(repository, _memoryCache, serviceLogger.Object, _configuration);

            var from = DateTime.UtcNow.Date.AddDays(-days);
            var to = DateTime.UtcNow.Date;

            // Query using aggregates
            var sw1 = Stopwatch.StartNew();
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            sw1.Stop();

            _output.WriteLine($"\n1st Query (Aggregates, No Cache):");
            _output.WriteLine($"  Time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Total Requests: {result1.TotalRequests:N0}");

            // Cached query
            var sw2 = Stopwatch.StartNew();
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);
            sw2.Stop();

            _output.WriteLine($"\n2nd Query (Cached):");
            _output.WriteLine($"  Time: {sw2.ElapsedMilliseconds}ms");

            var improvement = (1 - (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds) * 100;
            
            _output.WriteLine($"\n=== AGGREGATE + CACHE PERFORMANCE ===");
            _output.WriteLine($"  Aggregate query time: {sw1.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Cached query time: {sw2.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Cache improvement: {improvement:F1}%");
            _output.WriteLine($"  Data volume: 9M requests represented by 360 aggregate rows");
            _output.WriteLine($"  Compression ratio: {9000000.0 / 360:N0}:1");

            Assert.True(sw1.ElapsedMilliseconds < 500, "Aggregate query should be fast (< 500ms)");
            Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds, "Cached should be equal or faster");
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _memoryCache?.Dispose();
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
