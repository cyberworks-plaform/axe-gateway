using Ce.Gateway.Api.Models.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(DateTime startTime, DateTime endTime);
        Task<List<RouteSummaryDto>> GetRouteSummaryAsync(DateTime startTime, DateTime endTime);
        Task<List<NodeSummaryDto>> GetNodeSummaryAsync(DateTime startTime, DateTime endTime);
        Task<List<ErrorLogDto>> GetRecentErrorsAsync(DateTime startTime, DateTime endTime, int limit = 20);
        Task<(int totalNodes, int nodesDown)> GetNodeHealthStatsAsync();
        Task<List<NodeStatusWithMetricsDto>> GetNodeStatusWithMetricsAsync(DateTime startTime, DateTime endTime);
    }
}
