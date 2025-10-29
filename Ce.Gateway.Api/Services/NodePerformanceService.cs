using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public class NodePerformanceService : INodePerformanceService
    {
        private readonly ILogRepository _logRepository;

        public NodePerformanceService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        private async Task<IEnumerable<RequestLogEntry>> GetFilteredLogsAsync(DateTime? from, DateTime? to)
        {
            var filter = new LogFilter { From = from, To = to };
            // Fetch all logs within the time range for aggregation. 
            // Assuming a reasonable number of logs for in-memory aggregation.
            // For very large datasets, consider database-side aggregation.
            var allLogs = new List<RequestLogEntry>();
            int page = 1;
            int pageSize = 1000; // Fetch in chunks
            PaginatedResult<RequestLogEntry> paginatedResult;

            do
            {
                paginatedResult = await _logRepository.GetLogsAsync(filter, page, pageSize);
                allLogs.AddRange(paginatedResult.Data);
                page++;
            } while (paginatedResult.Data.Any() && allLogs.Count < paginatedResult.TotalCount);

            return allLogs;
        }

        public async Task<IEnumerable<NodePerformanceSummaryDto>> GetNodePerformanceSummaryAsync(DateTime? from, DateTime? to)
        {
            var logs = await GetFilteredLogsAsync(from, to);

            return logs.GroupBy(log => $"{log.DownstreamHost}:{log.DownstreamPort}")
                       .Select(g => new NodePerformanceSummaryDto
                       {
                           NodeIdentifier = g.Key,
                           TotalRequests = g.Count(),
                           SuccessfulRequests = g.Count(log => !log.IsError),
                           ErrorRequests = g.Count(log => log.IsError),
                           ErrorRate = g.Any() ? (double)g.Count(log => log.IsError) / g.Count() * 100 : 0,
                           MinLatencyMs = g.Any() ? g.Min(log => log.GatewayLatencyMs) : 0,
                           MaxLatencyMs = g.Any() ? g.Max(log => log.GatewayLatencyMs) : 0,
                           AvgLatencyMs = g.Any() ? g.Average(log => log.GatewayLatencyMs) : 0
                       })
                       .OrderBy(s => s.NodeIdentifier)
                       .ToList();
        }

        public async Task<IEnumerable<ChartDataDto>> GetRequestsPerNodeAsync(DateTime? from, DateTime? to)
        {
            var logs = await GetFilteredLogsAsync(from, to);

            return logs.GroupBy(log => $"{log.DownstreamHost}:{log.DownstreamPort}")
                       .Select(g => new ChartDataDto
                       {
                           Label = g.Key,
                           Value = g.Count()
                       })
                       .OrderBy(d => d.Label)
                       .ToList();
        }

        public async Task<IEnumerable<ChartDataDto>> GetAverageLatencyPerNodeAsync(DateTime? from, DateTime? to)
        {
            var logs = await GetFilteredLogsAsync(from, to);

            return logs.GroupBy(log => $"{log.DownstreamHost}:{log.DownstreamPort}")
                       .Where(g => g.Any())
                       .Select(g => new ChartDataDto
                       {
                           Label = g.Key,
                           Value = g.Average(log => log.GatewayLatencyMs)
                       })
                       .OrderBy(d => d.Label)
                       .ToList();
        }

        public async Task<IEnumerable<ChartDataDto>> GetErrorRatePerNodeAsync(DateTime? from, DateTime? to)
        {
            var logs = await GetFilteredLogsAsync(from, to);

            return logs.GroupBy(log => $"{log.DownstreamHost}:{log.DownstreamPort}")
                       .Where(g => g.Any())
                       .Select(g => new ChartDataDto
                       {
                           Label = g.Key,
                           Value = (double)g.Count(log => log.IsError) / g.Count() * 100
                       })
                       .OrderBy(d => d.Label)
                       .ToList();
        }

        public async Task<IEnumerable<NodeErrorLogDto>> GetTopNSlowestRequestsAsync(DateTime? from, DateTime? to, int n = 10)
        {
            var logs = await GetFilteredLogsAsync(from, to);

            return logs.Where(log => log.IsError)
                       .OrderByDescending(log => log.GatewayLatencyMs)
                       .Take(n)
                       .Select(log => new NodeErrorLogDto
                       {
                           CreatedAtUtc = log.CreatedAtUtc,
                           Route = $"{log.UpstreamHttpMethod} {log.UpstreamPath}", // Example route format
                           Node = $"{log.DownstreamHost}:{log.DownstreamPort}",
                           GatewayLatencyMs = log.GatewayLatencyMs,
                           DownstreamStatusCode = log.DownstreamStatusCode,
                           ErrorMessage = log.ErrorMessage,
                           RequestBody = log.RequestBody
                       })
                       .ToList();
        }
    }
}
