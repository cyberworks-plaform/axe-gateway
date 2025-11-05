using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using System.IO;

namespace Ce.Gateway.Api.Tests
{
    public class LogRepositoryTests
    {
        private readonly ITestOutputHelper _output;
        private readonly DbContextOptions<GatewayDbContext> _options;
        private readonly IMemoryCache _memoryCache;

        public LogRepositoryTests(ITestOutputHelper output)
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
        public async Task GetRequestReportAsync_ShouldReturnReport_For1Day()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-1);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var report = await repository.GetRequestReportAsync(from, now, "hour");
            stopwatch.Stop();

            _output.WriteLine($"Time for 1 day: {stopwatch.Elapsed.TotalMilliseconds}ms");
            Assert.NotNull(report);
            Assert.InRange(report.TotalRequests, 80000, 130000);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 3, $"Response time for 1 day ({stopwatch.Elapsed.TotalSeconds}s) exceeded 3 seconds.");
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldReturnReport_For7Days()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-7);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var report = await repository.GetRequestReportAsync(from, now, "day");
            stopwatch.Stop();

            _output.WriteLine($"Time for 7 days: {stopwatch.Elapsed.TotalMilliseconds}ms");
            Assert.NotNull(report);
            Assert.InRange(report.TotalRequests, 560000, 840000);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 5, $"Response time for 7 days ({stopwatch.Elapsed.TotalSeconds}s) exceeded 10 seconds.");
        }

        [Fact]
        public async Task GetRequestReportAsync_ShouldReturnReport_For30Days()
        {
            var dbContextFactory = new TestDbContextFactory(_options);
            var repository = new LogRepository(dbContextFactory, _memoryCache);

            var now = new DateTime(2025, 10, 31);
            var from = now.AddDays(-30);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var report = await repository.GetRequestReportAsync(from, now, "day");
            stopwatch.Stop();

            _output.WriteLine($"Time for 30 days: {stopwatch.Elapsed.TotalMilliseconds}ms");
            Assert.NotNull(report);
            Assert.InRange(report.TotalRequests, 2400000, 3600000);
            Assert.True(stopwatch.Elapsed.TotalSeconds < 20 , $"Response time for 30 days ({stopwatch.Elapsed.TotalSeconds}s) exceeded 35 seconds.");
        }
    }

    public class TestDbContextFactory : IDbContextFactory<GatewayDbContext>
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

        public Task<GatewayDbContext> CreateDbContextAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }
}