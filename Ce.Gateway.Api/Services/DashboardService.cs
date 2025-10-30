using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Dashboard;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ILogRepository _logRepository;
        private readonly IDownstreamHealthStore _downstreamHealthStore;

        public DashboardService(ILogRepository logRepository, IDownstreamHealthStore downstreamHealthStore)
        {
            _logRepository = logRepository;
            _downstreamHealthStore = downstreamHealthStore;
        }

        public async Task<(int totalNodes, int nodesDown)> GetNodeHealthStatsAsync()
        {
            var allHealths = await _downstreamHealthStore.GetAllHealthAsync();
            var totalNodes = allHealths.Count();
            var nodesDown = allHealths.Count(h => h.Status == "Unhealthy");
            return (totalNodes, nodesDown);
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(DateTime startTime, DateTime endTime)
        {
            var filter = new LogFilter { From = startTime, To = endTime };
            // Fetch all logs within the time range. Assuming a reasonable number for dashboard overview.
            // In a real-world scenario, this might need optimization for very large datasets.
            var allLogs = await _logRepository.GetLogsAsync(filter, 1, int.MaxValue);

            var logs = allLogs.Data.ToList();

            var totalRequests = logs.Count;
            var errorRequests = logs.Count(l => l.IsError);
            var errorRate = totalRequests > 0 ? (double)errorRequests / totalRequests * 100 : 0;
            var avgLatencyMs = totalRequests > 0 ? (long)Math.Round(logs.Average(l => l.GatewayLatencyMs)) : 0;

            var (totalNodes, nodesDown) = await GetNodeHealthStatsAsync(); // Get node health stats

            var requestTimeline = GenerateRequestTimeline(logs, startTime, endTime);
            var latencyTimeline = GenerateLatencyTimeline(logs, startTime, endTime);
            var httpStatusDistribution = GenerateHttpStatusDistribution(logs);

            return new DashboardOverviewDto
            {
                TotalNodes = totalNodes,
                NodesDown = nodesDown,
                TotalRequests = totalRequests,
                ErrorRate = errorRate,
                AvgLatencyMs = avgLatencyMs,
                RequestTimeline = requestTimeline,
                LatencyTimeline = latencyTimeline,
                HttpStatusDistribution = httpStatusDistribution
            };
        }

        private List<TimelineChartDataDto> GenerateLatencyTimeline(List<RequestLogEntry> logs, DateTime startTime, DateTime endTime)
        {
            var timeline = new List<TimelineChartDataDto>();

            TimeSpan duration = endTime - startTime;
            Func<DateTime, DateTime> grouper;
            string format;

            if (duration.TotalHours <= 1) // Less than or equal to 1 hour, group by minute
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
                format = "HH:mm";
            }
            else if (duration.TotalDays <= 1) // Less than or equal to 1 day, group by hour
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
                format = "HH:00";
            }
            else // More than 1 day, group by day
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
                format = "yyyy-MM-dd";
            }

            var groupedLogs = logs.GroupBy(l => grouper(l.CreatedAtUtc))
                                  .OrderBy(g => g.Key)
                                  .Select(g => new TimelineChartDataDto
                                  {
                                      Timestamp = g.Key.AddHours(7).ToString(format),
                                      RequestCount = (long)Math.Round(g.Average(l => l.GatewayLatencyMs))
                                  })
                                  .ToList();

            // Fill in missing intervals with 0 latency for a continuous timeline
            var current = grouper(startTime);
            while (current <= endTime)
            {
                if (!groupedLogs.Any(x => x.Timestamp == current.AddHours(7).ToString(format)))
                {
                    timeline.Add(new TimelineChartDataDto { Timestamp = current.AddHours(7).ToString(format), RequestCount = 0 });
                }
                current = GetNextInterval(current, duration);
            }
            timeline.AddRange(groupedLogs);
            return timeline.OrderBy(x => x.Timestamp).ToList();
        }

        public async Task<List<RouteSummaryDto>> GetRouteSummaryAsync(DateTime startTime, DateTime endTime)
        {
            var filter = new LogFilter { From = startTime, To = endTime };
            var allLogs = await _logRepository.GetLogsAsync(filter, 1, int.MaxValue);
            var logs = allLogs.Data.ToList();

            return logs.GroupBy(l => l.UpstreamPath)
                       .Select(g => new RouteSummaryDto
                       {
                           Route = g.Key,
                           MinLatencyMs = g.Min(l => l.GatewayLatencyMs),
                           MaxLatencyMs = g.Max(l => l.GatewayLatencyMs),
                           AvgLatencyMs = (long)Math.Round(g.Average(l => l.GatewayLatencyMs)),
                           TotalRequests = g.Count()
                       })
                       .OrderByDescending(r => r.TotalRequests)
                       .ToList();
        }

        public async Task<List<NodeSummaryDto>> GetNodeSummaryAsync(DateTime startTime, DateTime endTime)
        {
            var filter = new LogFilter { From = startTime, To = endTime };
            var allLogs = await _logRepository.GetLogsAsync(filter, 1, int.MaxValue);
            var logs = allLogs.Data.ToList();

            return logs.GroupBy(l => l.UpstreamHost)
                       .Select(g => new NodeSummaryDto
                       {
                           Node = g.Key,
                           MinLatencyMs = g.Min(l => l.GatewayLatencyMs),
                           MaxLatencyMs = g.Max(l => l.GatewayLatencyMs),
                           AvgLatencyMs = (long)Math.Round(g.Average(l => l.GatewayLatencyMs)),
                           TotalRequests = g.Count()
                       })
                       .OrderByDescending(n => n.TotalRequests)
                       .ToList();
        }

        public async Task<List<ErrorLogDto>> GetRecentErrorsAsync(DateTime startTime, DateTime endTime, int limit = 100)
        {
            var filter = new LogFilter { From = startTime, To = endTime };
            // Fetch more than limit to ensure we get the most recent after filtering
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
        }

        private List<TimelineChartDataDto> GenerateRequestTimeline(List<RequestLogEntry> logs, DateTime startTime, DateTime endTime)
        {
            var timeline = new List<TimelineChartDataDto>();

            // Determine appropriate time interval for grouping
            TimeSpan duration = endTime - startTime;
            Func<DateTime, DateTime> grouper;
            string format;

            if (duration.TotalHours <= 1) // Less than or equal to 1 hour, group by minute
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, DateTimeKind.Utc);
                format = "HH:mm";
            }
            else if (duration.TotalDays <= 1) // Less than or equal to 1 day, group by hour
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, DateTimeKind.Utc);
                format = "HH:00";
            }
            else // More than 1 day, group by day
            {
                grouper = dt => new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
                format = "yyyy-MM-dd";
            }

            var groupedLogs = logs.GroupBy(l => grouper(l.CreatedAtUtc))
                                  .OrderBy(g => g.Key)
                                  .Select(g => new TimelineChartDataDto
                                  {
                                      Timestamp = g.Key.AddHours(7).ToString(format),
                                      RequestCount = g.Count()
                                  })
                                  .ToList();

            // Fill in missing intervals with 0 requests for a continuous timeline
            var current = grouper(startTime);
            while (current <= endTime)
            {
                if (!groupedLogs.Any(x => x.Timestamp == current.AddHours(7).ToString(format)))
                {
                    timeline.Add(new TimelineChartDataDto { Timestamp = current.AddHours(7).ToString(format), RequestCount = 0 });
                }
                current = GetNextInterval(current, duration);
            }
            timeline.AddRange(groupedLogs);
            return timeline.OrderBy(x => x.Timestamp).ToList();
        }

        private DateTime GetNextInterval(DateTime current, TimeSpan duration)
        {
            if (duration.TotalHours <= 1)
            {
                return current.AddMinutes(1);
            }
            else if (duration.TotalDays <= 1)
            {
                return current.AddHours(1);
            }
            else
            {
                return current.AddDays(1);
            }
        }

        private List<DonutChartDataDto> GenerateHttpStatusDistribution(List<RequestLogEntry> logs)
        {
            return logs.Where(l => l.DownstreamStatusCode.HasValue)
                        .GroupBy(l => (l.DownstreamStatusCode.Value / 100) + "xx") // Group by 2xx, 3xx, 4xx, 5xx
                        .Select(g => new DonutChartDataDto
                        {
                            Label = g.Key,
                            Count = g.Count()
                        })
                        .OrderBy(d => d.Label)
                        .ToList();
        }

        public async Task<List<NodeStatusWithMetricsDto>> GetNodeStatusWithMetricsAsync(DateTime startTime, DateTime endTime)
        {
            var allHealths = await _downstreamHealthStore.GetAllHealthAsync();

            var filter = new LogFilter { From = startTime, To = endTime };
            var allLogs = await _logRepository.GetLogsAsync(filter, 1, int.MaxValue);
            var logs = allLogs.Data.ToList();

            var logsByNode = logs
                .Where(l => !string.IsNullOrEmpty(l.DownstreamHost) && l.DownstreamPort.HasValue)
                .GroupBy(l => $"{l.DownstreamHost}:{l.DownstreamPort}")
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<NodeStatusWithMetricsDto>();

            foreach (var health in allHealths)
            {
                var nodeKey = $"{health.Host}:{health.Port}";
                var hasLogs = logsByNode.TryGetValue(nodeKey, out var nodeLogs);

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

                if (hasLogs && nodeLogs.Any())
                {
                    nodeStatus.MinLatencyMs = nodeLogs.Min(l => l.GatewayLatencyMs);
                    nodeStatus.MaxLatencyMs = nodeLogs.Max(l => l.GatewayLatencyMs);
                    nodeStatus.AvgLatencyMs = (long)Math.Round(nodeLogs.Average(l => l.GatewayLatencyMs));
                    nodeStatus.TotalRequests = nodeLogs.Count;
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
        }
    }
}
