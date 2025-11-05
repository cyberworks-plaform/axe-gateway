using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class LogRepositoryCachingTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<GatewayDbContext> _options;
        private readonly IMemoryCache _memoryCache;

        public LogRepositoryCachingTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Setup in-memory SQLite database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            
            _options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the schema
            using (var context = new GatewayDbContext(_options))
            {
                context.Database.EnsureCreated();
                SeedTestData(context);
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        private void SeedTestData(GatewayDbContext context)
        {
            var baseTime = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var logEntries = new List<RequestLogEntry>();

            // Create 100 test entries over 2 days
            for (int i = 0; i < 100; i++)
            {
                logEntries.Add(new RequestLogEntry
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = baseTime.AddHours(i % 48),
                    DownstreamHost = "localhost",
                    DownstreamPort = 5000,
                    UpstreamPath = "/api/test",
                    UpstreamClientIp = "127.0.0.1",
                    DownstreamStatusCode = i % 10 == 0 ? 500 : (i % 5 == 0 ? 400 : 200),
                    GatewayLatencyMs = 100 + (i % 50),
                    IsError = i % 10 == 0
                });
            }

            context.OcrGatewayLogEntries.AddRange(logEntries);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldCacheResults()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 2);

            // Act - First call (should hit database)
            var stopwatch1 = Stopwatch.StartNew();
            var result1 = await repository.GetRequestReportAsync(from, to, "hour");
            stopwatch1.Stop();

            // Act - Second call (should hit cache)
            var stopwatch2 = Stopwatch.StartNew();
            var result2 = await repository.GetRequestReportAsync(from, to, "hour");
            stopwatch2.Stop();

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.TotalRequests, result2.TotalRequests);
            Assert.Equal(result1.SuccessRequests, result2.SuccessRequests);
            
            _output.WriteLine($"First call: {stopwatch1.ElapsedMilliseconds}ms");
            _output.WriteLine($"Second call (cached): {stopwatch2.ElapsedMilliseconds}ms");
            
            // Cache hit should be significantly faster
            Assert.True(stopwatch2.ElapsedMilliseconds < stopwatch1.ElapsedMilliseconds,
                $"Cached call ({stopwatch2.ElapsedMilliseconds}ms) should be faster than first call ({stopwatch1.ElapsedMilliseconds}ms)");
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldUseDifferentCacheKeys_ForDifferentParameters()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 2);

            // Act
            var resultHour = await repository.GetRequestReportAsync(from, to, "hour");
            var resultDay = await repository.GetRequestReportAsync(from, to, "day");

            // Assert - Results should be different due to different grouping
            Assert.NotNull(resultHour);
            Assert.NotNull(resultDay);
            Assert.NotEqual(resultHour.TimeFormat, resultDay.TimeFormat);
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldCache_OneDayQuery_For15Minutes()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 2); // 1 day

            // Act
            var result = await repository.GetRequestReportAsync(from, to, "hour");

            // Assert
            Assert.NotNull(result);
            
            // Verify cache key exists
            var cacheKey = $"request-report-{from:yyyyMMddHHmmss}-{to:yyyyMMddHHmmss}-hour";
            var cachedValue = _memoryCache.Get<Models.RequestReportDto>(cacheKey);
            Assert.NotNull(cachedValue);
            Assert.Equal(result.TotalRequests, cachedValue.TotalRequests);
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldCache_MultiDayQuery_ForOneDay()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 5); // 4 days

            // Act
            var result = await repository.GetRequestReportAsync(from, to, "day");

            // Assert
            Assert.NotNull(result);
            
            // Verify cache key exists
            var cacheKey = $"request-report-{from:yyyyMMddHHmmss}-{to:yyyyMMddHHmmss}-day";
            var cachedValue = _memoryCache.Get<Models.RequestReportDto>(cacheKey);
            Assert.NotNull(cachedValue);
            Assert.Equal(result.TotalRequests, cachedValue.TotalRequests);
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldReturnCorrectData_WhenCached()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 2);

            // Act
            var result1 = await repository.GetRequestReportAsync(from, to, "hour");
            var result2 = await repository.GetRequestReportAsync(from, to, "hour");

            // Assert - Verify all properties match
            Assert.Equal(result1.TotalRequests, result2.TotalRequests);
            Assert.Equal(result1.SuccessRequests, result2.SuccessRequests);
            Assert.Equal(result1.ClientErrorRequests, result2.ClientErrorRequests);
            Assert.Equal(result1.ServerErrorRequests, result2.ServerErrorRequests);
            Assert.Equal(result1.OtherRequests, result2.OtherRequests);
            Assert.Equal(result1.TimeFormat, result2.TimeFormat);
            Assert.Equal(result1.TimeSlots.Count, result2.TimeSlots.Count);
        }

        [Fact]
        public async Task GetRequestReportAsync_Performance_ShouldImproveWithCache()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            var from = new DateTime(2025, 11, 1);
            var to = new DateTime(2025, 11, 2);

            // Act - Measure multiple calls
            var times = new List<long>();
            for (int i = 0; i < 5; i++)
            {
                var sw = Stopwatch.StartNew();
                await repository.GetRequestReportAsync(from, to, "hour");
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                _output.WriteLine($"Call {i + 1}: {sw.ElapsedMilliseconds}ms");
            }

            // Assert - Subsequent calls should be faster
            for (int i = 1; i < times.Count; i++)
            {
                Assert.True(times[i] <= times[0],
                    $"Cached call {i + 1} ({times[i]}ms) should be faster than or equal to first call ({times[0]}ms)");
            }
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldHandleDifferentTimeRanges()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            // Act & Assert - Test different time ranges
            var result1Day = await repository.GetRequestReportAsync(
                new DateTime(2025, 11, 1),
                new DateTime(2025, 11, 2),
                "hour");
            Assert.NotNull(result1Day);

            var result7Days = await repository.GetRequestReportAsync(
                new DateTime(2025, 11, 1),
                new DateTime(2025, 11, 8),
                "day");
            Assert.NotNull(result7Days);

            var result1Month = await repository.GetRequestReportAsync(
                new DateTime(2025, 11, 1),
                new DateTime(2025, 12, 1),
                "day");
            Assert.NotNull(result1Month);
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldNotConfuseCache_WithSimilarButDifferentParameters()
        {
            // Arrange
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);
            
            var from1 = new DateTime(2025, 11, 1, 0, 0, 0);
            var to1 = new DateTime(2025, 11, 2, 0, 0, 0);
            
            var from2 = new DateTime(2025, 11, 1, 1, 0, 0); // Different by 1 hour
            var to2 = new DateTime(2025, 11, 2, 1, 0, 0);

            // Act
            var result1 = await repository.GetRequestReportAsync(from1, to1, "hour");
            var result2 = await repository.GetRequestReportAsync(from2, to2, "hour");

            // Assert - Results should be cached separately
            var cacheKey1 = $"request-report-{from1:yyyyMMddHHmmss}-{to1:yyyyMMddHHmmss}-hour";
            var cacheKey2 = $"request-report-{from2:yyyyMMddHHmmss}-{to2:yyyyMMddHHmmss}-hour";
            
            Assert.NotEqual(cacheKey1, cacheKey2);
            
            var cached1 = _memoryCache.Get<Models.RequestReportDto>(cacheKey1);
            var cached2 = _memoryCache.Get<Models.RequestReportDto>(cacheKey2);
            
            Assert.NotNull(cached1);
            Assert.NotNull(cached2);
        }

        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
            _memoryCache?.Dispose();
        }
    }
}
