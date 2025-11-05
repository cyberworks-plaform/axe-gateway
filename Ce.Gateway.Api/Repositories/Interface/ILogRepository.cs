using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Models.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories.Interface
{
    public interface ILogRepository
    {
        Task<PaginatedResult<RequestLogEntry>> GetLogsAsync(LogFilter filter, int page, int pageSize);
        Task<RequestReportDto> GetRequestReportAsync(DateTime from, DateTime to, string groupBy);
        Task<DashboardOverviewAggregateDto> GetDashboardOverviewAggregateAsync(DateTime startTime, DateTime endTime);
        Task<List<RouteSummaryDto>> GetRouteSummaryAggregateAsync(DateTime startTime, DateTime endTime);
        Task<List<NodeSummaryDto>> GetNodeSummaryAggregateAsync(DateTime startTime, DateTime endTime);
        Task<List<TimelineChartDataDto>> GetRequestTimelineAggregateAsync(DateTime startTime, DateTime endTime);
        Task<List<TimelineChartDataDto>> GetLatencyTimelineAggregateAsync(DateTime startTime, DateTime endTime);
        Task<List<DonutChartDataDto>> GetHttpStatusDistributionAsync(DateTime startTime, DateTime endTime);
    }
}
