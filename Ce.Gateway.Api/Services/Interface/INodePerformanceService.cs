using Ce.Gateway.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface INodePerformanceService
    {
        Task<IEnumerable<NodePerformanceSummaryDto>> GetNodePerformanceSummaryAsync(DateTime? from, DateTime? to);
        Task<IEnumerable<ChartDataDto>> GetRequestsPerNodeAsync(DateTime? from, DateTime? to);
        Task<IEnumerable<ChartDataDto>> GetAverageLatencyPerNodeAsync(DateTime? from, DateTime? to);
        Task<IEnumerable<ChartDataDto>> GetErrorRatePerNodeAsync(DateTime? from, DateTime? to);
        Task<IEnumerable<NodeErrorLogDto>> GetTopNSlowestRequestsAsync(DateTime? from, DateTime? to, int n = 10);
    }
}
