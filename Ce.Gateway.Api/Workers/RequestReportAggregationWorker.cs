using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Workers
{
    /// <summary>
    /// Background service that periodically aggregates request data
    /// into the RequestReportAggregates table for faster queries
    /// </summary>
    public class RequestReportAggregationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestReportAggregationWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _intervalMinutes;
        private readonly int _lookbackDays;
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);

        public RequestReportAggregationWorker(
            IServiceProvider serviceProvider,
            ILogger<RequestReportAggregationWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;

            // Load configuration
            _intervalMinutes = _configuration.GetValue("RequestReport:AggregationIntervalMinutes", 5);
            _lookbackDays = _configuration.GetValue("RequestReport:AggregationLookbackDays", 30); // Reduced from 90 to 30 for faster aggregation
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "RequestReportAggregationWorker started. Interval: {Interval} minutes, Lookback: {Lookback} days",
                _intervalMinutes, _lookbackDays);

            // Wait a bit before first run to let the application fully start
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Acquire lock before aggregation to prevent overlapping executions
                if (await _executionLock.WaitAsync(0, stoppingToken))
                {
                    try
                    {
                        await PerformAggregationAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during aggregation process");
                    }
                    finally
                    {
                        _executionLock.Release();
                    }
                }
                else
                {
                    _logger.LogWarning("Skipping aggregation - previous execution still running");
                }

                // Wait for the configured interval
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }

            _logger.LogInformation("RequestReportAggregationWorker stopped");
        }
        
        public override void Dispose()
        {
            _executionLock?.Dispose();
            base.Dispose();
        }

        private async Task PerformAggregationAsync(CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    _logger.LogInformation("Starting aggregation process (attempt {Attempt})", retryCount + 1);

                    using var scope = _serviceProvider.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IRequestReportRepository>();
                    var reportService = scope.ServiceProvider.GetRequiredService<IRequestReportService>();
                    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GatewayDbContext>>();

                    var startTime = DateTime.UtcNow;
                    var now = DateTime.UtcNow;
                    
                    // For hour aggregation, only look back 7 days (more recent, more frequent updates)
                    var hourLookbackDate = now.AddDays(-7).Date;
                    
                    // For day aggregation, look back configured days
                    var dayLookbackDate = now.AddDays(-_lookbackDays).Date;
                    
                    // For month aggregation, look back 12 months
                    var monthLookbackDate = now.AddMonths(-12);
                    monthLookbackDate = new DateTime(monthLookbackDate.Year, monthLookbackDate.Month, 1);

                    // Aggregate by hour (for recent data, real-time requirements)
                    await AggregateByGranularityAsync(
                        dbContextFactory, 
                        repository, 
                        reportService,
                        hourLookbackDate, 
                        Granularity.Hour, 
                        cancellationToken);

                    // Aggregate by day
                    await AggregateByGranularityAsync(
                        dbContextFactory, 
                        repository, 
                        reportService,
                        dayLookbackDate, 
                        Granularity.Day, 
                        cancellationToken);

                    // Aggregate by month
                    await AggregateByGranularityAsync(
                        dbContextFactory, 
                        repository, 
                        reportService,
                        monthLookbackDate, 
                        Granularity.Month, 
                        cancellationToken);

                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation("Aggregation process completed in {Duration}ms", duration.TotalMilliseconds);
                    
                    return; // Success - exit
                }
                catch (DbUpdateException ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount)); // Exponential backoff: 2s, 4s, 8s
                    _logger.LogWarning(ex, "Aggregation failed, retrying in {Delay}s (attempt {Attempt}/{Max})", 
                        delay.TotalSeconds, retryCount, maxRetries);
                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fatal error during aggregation process");
                    throw;
                }
            }
        }

        private async Task AggregateByGranularityAsync(
            IDbContextFactory<GatewayDbContext> dbContextFactory,
            IRequestReportRepository repository,
            IRequestReportService reportService,
            DateTime lookbackDate,
            Granularity granularity,
            CancellationToken cancellationToken)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var periods = GetPeriodsToAggregate(lookbackDate, granularity);

            _logger.LogDebug("Aggregating {Count} periods for granularity {Granularity}", 
                periods.Count, granularity);

            foreach (var periodStart in periods)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var periodEnd = GetPeriodEnd(periodStart, granularity);

                    // Query raw data for this period
                    var sql = @"
                        SELECT 
                            CASE 
                                WHEN DownstreamStatusCode >= 200 AND DownstreamStatusCode < 300 THEN 2
                                WHEN DownstreamStatusCode >= 400 AND DownstreamStatusCode < 500 THEN 4
                                WHEN DownstreamStatusCode >= 500 AND DownstreamStatusCode < 600 THEN 5
                                ELSE 0
                            END as StatusCategory,
                            COUNT(*) as Count
                        FROM OcrGatewayLogEntries
                        WHERE CreatedAtUtc >= @p0 AND CreatedAtUtc < @p1
                        GROUP BY StatusCategory";

                    var results = await dbContext.Database
                        .SqlQueryRaw<StatusCategoryCount>(sql, periodStart, periodEnd)
                        .ToListAsync(cancellationToken);

                    // Build status counts dictionary
                    var statusCounts = new Dictionary<int, long>
                    {
                        { 0, 0 }, // Other
                        { 2, 0 }, // Success (2xx)
                        { 4, 0 }, // Client Error (4xx)
                        { 5, 0 }  // Server Error (5xx)
                    };

                    foreach (var result in results)
                    {
                        statusCounts[result.StatusCategory] = result.Count;
                    }

                    // Only upsert if there's data or if the aggregate already exists
                    var hasData = statusCounts.Values.Any(c => c > 0);
                    if (hasData)
                    {
                        await repository.UpsertAggregatesAsync(periodStart, granularity, statusCounts);
                        
                        // Invalidate cache for this period
                        reportService.InvalidateCache(periodStart, periodEnd);
                        
                        _logger.LogDebug("Aggregated period {Period} ({Granularity}): {Total} total requests",
                            periodStart, granularity, statusCounts.Values.Sum());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error aggregating period {Period} ({Granularity})", 
                        periodStart, granularity);
                }
            }
        }

        private List<DateTime> GetPeriodsToAggregate(DateTime lookbackDate, Granularity granularity)
        {
            var periods = new List<DateTime>();
            var current = lookbackDate;
            var now = DateTime.UtcNow;

            // Normalize current based on granularity
            current = granularity switch
            {
                Granularity.Hour => new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0),
                Granularity.Day => current.Date,
                Granularity.Month => new DateTime(current.Year, current.Month, 1),
                _ => current.Date
            };

            while (current <= now)
            {
                periods.Add(current);
                
                current = granularity switch
                {
                    Granularity.Hour => current.AddHours(1),
                    Granularity.Day => current.AddDays(1),
                    Granularity.Month => current.AddMonths(1),
                    _ => current.AddDays(1)
                };
            }

            return periods;
        }

        private DateTime GetPeriodEnd(DateTime periodStart, Granularity granularity)
        {
            return granularity switch
            {
                Granularity.Hour => periodStart.AddHours(1),
                Granularity.Day => periodStart.AddDays(1),
                Granularity.Month => periodStart.AddMonths(1),
                _ => periodStart.AddDays(1)
            };
        }

        // Helper class for query result
        private class StatusCategoryCount
        {
            public int StatusCategory { get; set; }
            public long Count { get; set; }
        }
    }
}
