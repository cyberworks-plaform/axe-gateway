using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class LogRepositoryOptimizedTests
    {
        private readonly ITestOutputHelper _output;
        private readonly DbContextOptions<GatewayDbContext> _options;
        private readonly IMemoryCache _memoryCache;

        public LogRepositoryOptimizedTests(ITestOutputHelper output)
        {
            _output = output;
            var dbPath = Path.Combine("..", "..", "..", "..", "Ce.Gateway.Api", "Data", "gateway.development.db");
            var connectionString = $"DataSource={dbPath}";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            _options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite(connection)
                .Options;
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        [Fact]
        public async Task GetDashboardOverviewAggregateAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetDashboardOverviewAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetDashboardOverviewAggregateAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRequestTimelineAggregateAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetRequestTimelineAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetRequestTimelineAggregateAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetLatencyTimelineAggregateAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetLatencyTimelineAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetLatencyTimelineAggregateAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRouteSummaryAggregateAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetRouteSummaryAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetRouteSummaryAggregateAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetNodeSummaryAggregateAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetNodeSummaryAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetNodeSummaryAggregateAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetHttpStatusDistributionAsync_1Hour_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddHours(-1);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetHttpStatusDistributionAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetHttpStatusDistributionAsync 1 Hour: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRequestTimelineAggregateAsync_5Minutes_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddMinutes(-5);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetRequestTimelineAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetRequestTimelineAggregateAsync 5 Minutes: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRequestTimelineAggregateAsync_15Minutes_ShouldNotThrowException()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31, 12, 0, 0);
            var from = now.AddMinutes(-15);

            var stopwatch = Stopwatch.StartNew();
            var result = await repository.GetRequestTimelineAggregateAsync(from, now);
            stopwatch.Stop();

            _output.WriteLine($"GetRequestTimelineAggregateAsync 15 Minutes: {stopwatch.Elapsed.TotalMilliseconds}ms");
            _output.WriteLine($"Result count: {result.Count}");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetRequestTimelineAggregateAsync_EmptyResult_ShouldReturnEmptyList()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2099, 1, 1);
            var from = now.AddHours(-1);

            var result = await repository.GetRequestTimelineAggregateAsync(from, now);

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
