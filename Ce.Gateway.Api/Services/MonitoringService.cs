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

                // Upstream Information
                UpstreamHost = entry.UpstreamHost,
                UpstreamPort = entry.UpstreamPort,
                UpstreamScheme = entry.UpstreamScheme,
                UpstreamHttpMethod = entry.UpstreamHttpMethod,
                UpstreamPathTemplate = entry.UpstreamPathTemplate,
                UpstreamPath = entry.UpstreamPath,
                UpstreamQueryString = entry.UpstreamQueryString,
                UpstreamRequestSize = entry.UpstreamRequestSize,
                UpstreamClientIp = entry.UpstreamClientIp,

                // Downstream Information
                DownstreamScheme = entry.DownstreamScheme,
                DownstreamHost = entry.DownstreamHost,
                DownstreamPort = entry.DownstreamPort,
                DownstreamPathTemplate = entry.DownstreamPathTemplate,
                DownstreamPath = entry.DownstreamPath,
                DownstreamQueryString = entry.DownstreamQueryString,
                DownstreamRequestSize = entry.DownstreamRequestSize,
                DownstreamResponseSize = entry.DownstreamResponseSize,
                DownstreamStatusCode = entry.DownstreamStatusCode,

                // Gateway information
                GatewayLatencyMs = entry.GatewayLatencyMs,
                IsError = entry.IsError,
                ErrorMessage = entry.ErrorMessage
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
