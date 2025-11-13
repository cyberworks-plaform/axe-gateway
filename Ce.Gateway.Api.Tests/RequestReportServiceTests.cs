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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class RequestReportServiceTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SqliteConnection _connection;
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<RequestReportRepository>> _repoLoggerMock;
        private readonly Mock<ILogger<RequestReportService>> _serviceLoggerMock;

        public RequestReportServiceTests(ITestOutputHelper output)
        {
            _output = output;
            
            // Setup in-memory SQLite database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            
            var options = new DbContextOptionsBuilder<GatewayDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create factory
            _dbContextFactory = new TestDbContextFactory(options);

            // Create the schema
            using (var context = _dbContextFactory.CreateDbContext())
            {
                context.Database.EnsureCreated();
                SeedTestData(context);
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

            _repoLoggerMock = new Mock<ILogger<RequestReportRepository>>();
            _serviceLoggerMock = new Mock<ILogger<RequestReportService>>();
        }

        private void SeedTestData(GatewayDbContext context)
        {
            var baseTime = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var logEntries = new List<RequestLogEntry>();

            // Create test data for 3 days with different status codes
            for (int day = 0; day < 3; day++)
            {
                var dayStart = baseTime.AddDays(day);
                
                // Success requests (200)
                for (int i = 0; i < 10; i++)
                {
                    logEntries.Add(new RequestLogEntry
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = dayStart.AddHours(i),
                        DownstreamStatusCode = 200,
                        UpstreamPath = "/api/test",
                        DownstreamHost = "test-host"
                    });
                }

                // Client errors (404)
                for (int i = 0; i < 3; i++)
                {
                    logEntries.Add(new RequestLogEntry
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = dayStart.AddHours(i),
                        DownstreamStatusCode = 404,
                        UpstreamPath = "/api/test",
                        DownstreamHost = "test-host"
                    });
                }

                // Server errors (500)
                for (int i = 0; i < 2; i++)
                {
                    logEntries.Add(new RequestLogEntry
                    {
                        Id = Guid.NewGuid(),
                        CreatedAtUtc = dayStart.AddHours(i),
                        DownstreamStatusCode = 500,
                        UpstreamPath = "/api/test",
                        DownstreamHost = "test-host"
                    });
                }
            }

            context.OcrGatewayLogEntries.AddRange(logEntries);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetReportAsync_ShouldReturnData_WhenCalledFirstTime()
        {
            // Arrange
            var repository = new RequestReportRepository(_dbContextFactory, _repoLoggerMock.Object);
            var service = new RequestReportService(
                repository, 
                _memoryCache, 
                _serviceLoggerMock.Object, 
                _configuration);

            var from = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 11, 3, 23, 59, 59, DateTimeKind.Utc);

            // Act - Use Hour granularity to force raw query (no aggregates in test DB)
            var result = await service.GetReportAsync(from, to, Granularity.Hour);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalRequests > 0, "Should have some requests");
            Assert.True(result.SuccessRequests > 0, "Should have some success requests");
            
            _output.WriteLine($"Total Requests: {result.TotalRequests}");
            _output.WriteLine($"Success: {result.SuccessRequests}, Client Error: {result.ClientErrorRequests}, Server Error: {result.ServerErrorRequests}");
        }

        [Fact]
        public async Task GetReportAsync_ShouldUseCacheOnSecondCall()
        {
            // Arrange
            var repository = new RequestReportRepository(_dbContextFactory, _repoLoggerMock.Object);
            var service = new RequestReportService(
                repository, 
                _memoryCache, 
                _serviceLoggerMock.Object, 
                _configuration);

            var from = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 11, 3, 23, 59, 59, DateTimeKind.Utc);

            // Act - First call
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            
            // Act - Second call (should hit cache)
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.TotalRequests, result2.TotalRequests);
            Assert.Same(result1, result2); // Should be the exact same object from cache
            
            _output.WriteLine("Cache hit confirmed - same object returned");
        }

        [Fact]
        public async Task InvalidateCache_ShouldClearCachedData()
        {
            // Arrange
            var repository = new RequestReportRepository(_dbContextFactory, _repoLoggerMock.Object);
            var service = new RequestReportService(
                repository, 
                _memoryCache, 
                _serviceLoggerMock.Object, 
                _configuration);

            var from = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 11, 3, 23, 59, 59, DateTimeKind.Utc);

            // Act - First call to populate cache
            var result1 = await service.GetReportAsync(from, to, Granularity.Day);
            
            // Invalidate cache
            service.InvalidateCache(from, to);
            
            // Second call (should not hit cache)
            var result2 = await service.GetReportAsync(from, to, Granularity.Day);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1.TotalRequests, result2.TotalRequests);
            Assert.NotSame(result1, result2); // Should be different objects (cache was invalidated)
            
            _output.WriteLine("Cache invalidation confirmed - different objects returned");
        }

        [Fact]
        public async Task GetReportAsync_ShouldUseRawQuery_ForHourlyGranularity()
        {
            // Arrange
            var repository = new RequestReportRepository(_dbContextFactory, _repoLoggerMock.Object);
            var service = new RequestReportService(
                repository, 
                _memoryCache, 
                _serviceLoggerMock.Object, 
                _configuration);

            var from = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 11, 1, 23, 59, 59, DateTimeKind.Utc);

            // Act
            var result = await service.GetReportAsync(from, to, Granularity.Hour);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalRequests > 0);
            Assert.Equal("HH:00", result.TimeFormat);
            
            _output.WriteLine($"Hourly report - Total Requests: {result.TotalRequests}");
        }

        [Fact]
        public async Task GetReportAsync_WithFilters_ShouldUseRawQuery()
        {
            // Arrange
            var repository = new RequestReportRepository(_dbContextFactory, _repoLoggerMock.Object);
            var service = new RequestReportService(
                repository, 
                _memoryCache, 
                _serviceLoggerMock.Object, 
                _configuration);

            var from = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2025, 11, 3, 23, 59, 59, DateTimeKind.Utc);
            var filter = new ReportFilter { UpstreamPath = "/api/test" };

            // Act
            var result = await service.GetReportAsync(from, to, Granularity.Day, filter);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalRequests > 0);
            
            _output.WriteLine($"Filtered report - Total Requests: {result.TotalRequests}");
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _memoryCache?.Dispose();
        }

        // Helper class for DbContext factory
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
