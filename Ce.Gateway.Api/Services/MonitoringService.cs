using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public class MonitoringService : IMonitoringService
    {
        private readonly ILogRepository _logRepository;

        public MonitoringService(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        public async Task<PaginatedResult<LogDto>> GetLogsAsync(LogFilter filter, int page, int pageSize)
        {
            var paginatedEntries = await _logRepository.GetLogsAsync(filter, page, pageSize);

            var logDtos = paginatedEntries.Data.Select(entry => new LogDto
            {
                Id = entry.Id,
                CreatedAtUtc = entry.CreatedAtUtc,
                TraceId = entry.TraceId,
                Route = entry.Route,
                Method = entry.Method,
                Path = entry.Path,
                DownstreamNode = entry.DownstreamNode,
                StatusCode = entry.StatusCode,
                LatencyMs = entry.LatencyMs,
                ServiceApi = entry.ServiceApi,
                Client = entry.Client,
                Error = entry.Error
            }).ToList();

            return new PaginatedResult<LogDto>
            {
                TotalCount = paginatedEntries.TotalCount,
                TotalPages = paginatedEntries.TotalPages,
                Page = paginatedEntries.Page,
                PageSize = paginatedEntries.PageSize,
                Data = logDtos
            };
        }
    }
}
