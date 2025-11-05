using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services;
using Ce.Gateway.Api.Services.Interface;
using Ce.Gateway.Api.Workers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Ce.Gateway.Api.Tests
{
    public class RequestReportAggregationWorkerTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly SqliteConnection _connection;
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IServiceProvider _serviceProvider;

        public RequestReportAggregationWorkerTests(ITestOutputHelper output)
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
                SeedTestData(context);
            }

            // Setup service provider
            var services = new ServiceCollection();
            
            // Add configuration
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RequestReport:AggregationIntervalMinutes", "1" },
                { "RequestReport:AggregationLookbackDays", "7" },
                { "RequestReport:CacheDefaultTtlMinutes", "30" },
                { "RequestReport:CacheShortTtlMinutes", "2" }
            });
            services.AddSingleton<IConfiguration>(configBuilder.Build());

            // Add services
            services.AddSingleton(_dbContextFactory);
            services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
            services.AddSingleton<IRequestReportRepository, RequestReportRepository>();
            services.AddSingleton<IRequestReportService, RequestReportService>();
            
            // Add logging
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();
        }

        private void SeedTestData(GatewayDbContext context)
        {
            var baseTime = DateTime.UtcNow.Date.AddDays(-2);
            var logEntries = new List<RequestLogEntry>();

            // Create test data for 2 days
            for (int day = 0; day < 2; day++)
            {
                var dayStart = baseTime.AddDays(day);
                
                // Success requests (200)
                for (int i = 0; i < 5; i++)
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
                for (int i = 0; i < 2; i++)
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
            }

            context.OcrGatewayLogEntries.AddRange(logEntries);
            context.SaveChanges();
        }

        [Fact]
        public async Task UpsertAggregatesAsync_ShouldStoreAggregates()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRequestReportRepository>();
            var periodStart = DateTime.UtcNow.Date.AddDays(-1);
            
            var statusCounts = new Dictionary<int, long>
            {
                { 2, 100 }, // Success
                { 4, 20 },  // Client Error
                { 5, 5 },   // Server Error
                { 0, 1 }    // Other
            };

            // Act
            await repository.UpsertAggregatesAsync(periodStart, Granularity.Day, statusCounts);

            // Assert - Query back the aggregates
            using var context = _dbContextFactory.CreateDbContext();
            var aggregates = await context.RequestReportAggregates
                .Where(a => a.PeriodStart == periodStart && a.Granularity == "day")
                .ToListAsync();

            Assert.Equal(4, aggregates.Count);
            Assert.Contains(aggregates, a => a.StatusCategory == 2 && a.Count == 100);
            Assert.Contains(aggregates, a => a.StatusCategory == 4 && a.Count == 20);
            Assert.Contains(aggregates, a => a.StatusCategory == 5 && a.Count == 5);
            Assert.Contains(aggregates, a => a.StatusCategory == 0 && a.Count == 1);

            _output.WriteLine("Aggregates successfully stored in database");
        }

        [Fact]
        public async Task GetAggregatedCountsAsync_ShouldReturnStoredAggregates()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRequestReportRepository>();
            var periodStart = DateTime.UtcNow.Date.AddDays(-1);
            
            // Store some aggregates first
            var statusCounts = new Dictionary<int, long>
            {
                { 2, 50 },
                { 4, 10 },
                { 5, 5 },
                { 0, 0 }
            };
            await repository.UpsertAggregatesAsync(periodStart, Granularity.Day, statusCounts);

            // Act
            var result = await repository.GetAggregatedCountsAsync(
                periodStart, 
                periodStart, 
                Granularity.Day, 
                new ReportFilter());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(65, result.TotalRequests);
            Assert.Equal(50, result.SuccessRequests);
            Assert.Equal(10, result.ClientErrorRequests);
            Assert.Equal(5, result.ServerErrorRequests);

            _output.WriteLine($"Retrieved aggregates - Total: {result.TotalRequests}");
        }

        [Fact]
        public async Task GetAggregatesLastUpdatedAsync_ShouldReturnLatestTimestamp()
        {
            // Arrange
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRequestReportRepository>();
            var periodStart = DateTime.UtcNow.Date.AddDays(-1);
            
            var statusCounts = new Dictionary<int, long> { { 2, 10 } };
            await repository.UpsertAggregatesAsync(periodStart, Granularity.Day, statusCounts);

            // Act
            var lastUpdated = await repository.GetAggregatesLastUpdatedAsync(
                periodStart, 
                periodStart, 
                Granularity.Day);

            // Assert
            Assert.NotNull(lastUpdated);
            Assert.True(lastUpdated.Value >= DateTime.UtcNow.AddMinutes(-1));

            _output.WriteLine($"Last updated: {lastUpdated}");
        }

        public void Dispose()
        {
            _connection?.Dispose();
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
