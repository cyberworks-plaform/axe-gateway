using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Dashboard;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    /// <summary>
    /// Dashboard service with integrated in-memory caching for improved performance
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ILogRepository _logRepository;
        private readonly IDownstreamHealthStore _downstreamHealthStore;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private const string CACHE_PREFIX = "dashboard:";

        public DashboardService(
            ILogRepository logRepository,
            IDownstreamHealthStore downstreamHealthStore,
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _logRepository = logRepository;
            _downstreamHealthStore = downstreamHealthStore;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(int totalNodes, int nodesDown)> GetNodeHealthStatsAsync()
        {
            var cacheKey = $"{CACHE_PREFIX}node_health_stats";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);

                var allHealths = await _downstreamHealthStore.GetAllHealthAsync();
                var totalNodes = allHealths.Count();
                var nodesDown = allHealths.Count(h => h.Status == "Unhealthy");

                _logger.LogDebug("Node health stats cached: {TotalNodes} nodes, {NodesDown} down", totalNodes, nodesDown);
                return (totalNodes, nodesDown);
            });
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                // Ensure UTC
                if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
                if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

                // Validate time range
                if (endTime <= startTime)
                {
                    throw new ArgumentException("endTime must be greater than startTime");
                }

                var cacheKey = GetCacheKey("overview", startTime, endTime);
                var cacheDuration = GetCacheDuration(startTime, endTime);

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                    _logger.LogDebug("Cache miss for dashboard overview. Fetching from database...");

                    // Execute all queries in parallel
                    var aggregateTask = _logRepository.GetDashboardOverviewAggregateAsync(startTime, endTime);
                    var requestTimelineTask = _logRepository.GetRequestTimelineAggregateAsync(startTime, endTime);
                    var latencyTimelineTask = _logRepository.GetLatencyTimelineAggregateAsync(startTime, endTime);
                    var httpStatusTask = _logRepository.GetHttpStatusDistributionAsync(startTime, endTime);
                    var nodeHealthTask = GetNodeHealthStatsAsync();

                    await Task.WhenAll(aggregateTask, requestTimelineTask, latencyTimelineTask, httpStatusTask, nodeHealthTask);

                    var aggregate = await aggregateTask;
                    var requestTimeline = await requestTimelineTask;
                    var latencyTimeline = await latencyTimelineTask;
                    var httpStatusDistribution = await httpStatusTask;
                    var (totalNodes, nodesDown) = await nodeHealthTask;

                    var errorRate = aggregate.TotalRequests > 0 ? (double)aggregate.ErrorRequests / aggregate.TotalRequests * 100 : 0;
                    var avgLatencyMs = aggregate.TotalRequests > 0 ? aggregate.TotalLatencyMs / aggregate.TotalRequests : 0;

                    return new DashboardOverviewDto
                    {
                        TotalNodes = totalNodes,
                        NodesDown = nodesDown,
                        TotalRequests = aggregate.TotalRequests,
                        ErrorRate = errorRate,
                        AvgLatencyMs = avgLatencyMs,
                        RequestTimeline = requestTimeline ?? new List<TimelineChartDataDto>(),
                        LatencyTimeline = latencyTimeline ?? new List<TimelineChartDataDto>(),
                        HttpStatusDistribution = httpStatusDistribution ?? new List<DonutChartDataDto>()
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardOverviewAsync");
                throw;
            }
        }

        public async Task<List<RouteSummaryDto>> GetRouteSummaryAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
                if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

                var cacheKey = GetCacheKey("route_summary", startTime, endTime);
                var cacheDuration = GetCacheDuration(startTime, endTime);

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;
                    return await _logRepository.GetRouteSummaryAggregateAsync(startTime, endTime);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRouteSummaryAsync");
                throw;
            }
        }

        public async Task<List<NodeSummaryDto>> GetNodeSummaryAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
                if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

                var cacheKey = GetCacheKey("node_summary", startTime, endTime);
                var cacheDuration = GetCacheDuration(startTime, endTime);

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;
                    return await _logRepository.GetNodeSummaryAggregateAsync(startTime, endTime);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNodeSummaryAsync");
                throw;
            }
        }

        public async Task<List<ErrorLogDto>> GetRecentErrorsAsync(DateTime startTime, DateTime endTime, int limit = 20)
        {
            try
            {
                if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
                if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

                var cacheKey = GetCacheKey($"recent_errors_{limit}", startTime, endTime);
                var cacheDuration = GetCacheDuration(startTime, endTime);

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                    var filter = new LogFilter { From = startTime, To = endTime };
                    var allLogs = await _logRepository.GetLogsAsync(filter, 1, limit * 2);

                    return allLogs.Data.Where(l => l.IsError)
                                       .OrderByDescending(l => l.CreatedAtUtc)
                                       .Take(limit)
                                       .Select(l => new ErrorLogDto
                                       {
                                           Id = l.Id,
                                           CreatedAtUtc = l.CreatedAtUtc,
                                           UpstreamPath = l.UpstreamPath,
                                           UpstreamHost = l.UpstreamHost,
                                           ErrorMessage = l.ErrorMessage,
                                           GatewayLatencyMs = l.GatewayLatencyMs,
                                           DownstreamStatusCode = l.DownstreamStatusCode,
                                           RequestBody = l.RequestBody
                                       })
                                       .ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecentErrorsAsync");
                throw;
            }
        }

        public async Task<List<NodeStatusWithMetricsDto>> GetNodeStatusWithMetricsAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                if (startTime.Kind != DateTimeKind.Utc) startTime = startTime.ToUniversalTime();
                if (endTime.Kind != DateTimeKind.Utc) endTime = endTime.ToUniversalTime();

                var cacheKey = GetCacheKey("node_status_metrics", startTime, endTime);
                var cacheDuration = GetCacheDuration(startTime, endTime);

                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = cacheDuration;

                    var allHealths = await _downstreamHealthStore.GetAllHealthAsync();
                    var nodeSummary = await _logRepository.GetNodeSummaryAggregateAsync(startTime, endTime);

                    var nodeSummaryDict = nodeSummary.ToDictionary(n => n.Node, n => n);
                    var result = new List<NodeStatusWithMetricsDto>();

                    foreach (var health in allHealths)
                    {
                        var nodeKey = $"{health.Host}:{health.Port}";
                        var hasMetrics = nodeSummaryDict.TryGetValue(nodeKey, out var metrics);

                        var nodeStatus = new NodeStatusWithMetricsDto
                        {
                            Node = nodeKey,
                            IsHealthy = health.Status == "Healthy",
                            Status = health.Status,
                            LastChecked = health.LastChecked,
                            StatusMessage = health.StatusMessage,
                            TotalDuration = health.TotalDuration,
                            Entries = health.Entries?.ToDictionary(
                                e => e.Key,
                                e => new HealthCheckEntryDto
                                {
                                    Status = e.Value.Status,
                                    Description = e.Value.Description,
                                    Duration = e.Value.Duration
                                })
                        };

                        if (hasMetrics)
                        {
                            nodeStatus.MinLatencyMs = metrics.MinLatencyMs;
                            nodeStatus.MaxLatencyMs = metrics.MaxLatencyMs;
                            nodeStatus.AvgLatencyMs = metrics.AvgLatencyMs;
                            nodeStatus.TotalRequests = metrics.TotalRequests;
                        }
                        else
                        {
                            nodeStatus.MinLatencyMs = 0;
                            nodeStatus.MaxLatencyMs = 0;
                            nodeStatus.AvgLatencyMs = 0;
                            nodeStatus.TotalRequests = 0;
                        }

                        result.Add(nodeStatus);
                    }

                    return result.OrderByDescending(n => n.TotalRequests).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNodeStatusWithMetricsAsync");
                throw;
            }
        }

        #region Cache Helper Methods

        private string GetCacheKey(string operation, DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            var timeRange = GetTimeRangeCategory(duration);

            // Normalize timestamps based on time range for better cache hit rate
            var (normalizedStart, normalizedEnd) = NormalizeTimeRange(startTime, endTime, duration);

            return $"{CACHE_PREFIX}{operation}:{timeRange}:{normalizedStart:yyyyMMddHHmm}:{normalizedEnd:yyyyMMddHHmm}";
        }

        private string GetTimeRangeCategory(TimeSpan duration)
        {
            if (duration.TotalMinutes <= 15) return "15m";
            if (duration.TotalMinutes <= 30) return "30m";
            if (duration.TotalHours <= 1) return "1h";
            if (duration.TotalHours <= 3) return "3h";
            if (duration.TotalHours <= 6) return "6h";
            if (duration.TotalHours <= 12) return "12h";
            if (duration.TotalDays <= 1) return "1d";
            if (duration.TotalDays <= 7) return "7d";
            if (duration.TotalDays <= 30) return "30d";
            return "max";
        }

        private (DateTime normalizedStart, DateTime normalizedEnd) NormalizeTimeRange(DateTime start, DateTime end, TimeSpan duration)
        {
            if (duration.TotalMinutes <= 15)
            {
                var normalizedStart = RoundToMinutes(start, 5);
                var normalizedEnd = normalizedStart.AddMinutes(15);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalMinutes <= 30)
            {
                var normalizedStart = RoundToMinutes(start, 5);
                var normalizedEnd = normalizedStart.AddMinutes(30);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalHours <= 1)
            {
                var normalizedStart = RoundToMinutes(start, 10);
                var normalizedEnd = normalizedStart.AddHours(1);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalHours <= 3)
            {
                var normalizedStart = RoundToMinutes(start, 15);
                var normalizedEnd = normalizedStart.AddHours(3);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalHours <= 12)
            {
                var normalizedStart = RoundToMinutes(start, 30);
                var normalizedEnd = normalizedStart.AddHours(duration.TotalHours);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalDays <= 1)
            {
                var normalizedStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, DateTimeKind.Utc);
                var normalizedEnd = normalizedStart.AddDays(1);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalDays <= 7)
            {
                var normalizedStart = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
                var normalizedEnd = normalizedStart.AddDays(7);
                return (normalizedStart, normalizedEnd);
            }
            else if (duration.TotalDays <= 30)
            {
                var normalizedStart = new DateTime(start.Year, start.Month, start.Day, 0, 0, 0, DateTimeKind.Utc);
                var normalizedEnd = normalizedStart.AddDays(30);
                return (normalizedStart, normalizedEnd);
            }
            else
            {
                var normalizedStart = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var normalizedEnd = normalizedStart.AddMonths(1);
                return (normalizedStart, normalizedEnd);
            }
        }

        private DateTime RoundToMinutes(DateTime dateTime, int minutes)
        {
            var roundedMinutes = (dateTime.Minute / minutes) * minutes;
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, roundedMinutes, 0, DateTimeKind.Utc);
        }

        private TimeSpan GetCacheDuration(DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            var now = DateTime.UtcNow;
            var timeSinceEnd = now - endTime;

            // Real-time strategy: Use short cache for recent data
            // Matching /requestreport cache strategy for consistency
            if (duration.TotalHours <= 1 || timeSinceEnd.TotalMinutes <= 10)
            {
                // Real-time data (≤1h range or last 10 minutes): 30s cache
                return TimeSpan.FromSeconds(30);
            }
            else if (duration.TotalDays <= 1)
            {
                // Recent data (≤1 day): 2 min cache
                return TimeSpan.FromMinutes(2);
            }
            else if (duration.TotalDays <= 7)
            {
                // Weekly data: 10 min cache
                return TimeSpan.FromMinutes(10);
            }
            else if (duration.TotalDays <= 30)
            {
                // Monthly data: 30 min cache
                return TimeSpan.FromMinutes(30);
            }
            else
            {
                // Historical data (>30 days): 6 hour cache
                return TimeSpan.FromHours(6);
            }
        }

        #endregion
    }
}
