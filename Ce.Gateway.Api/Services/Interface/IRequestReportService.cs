using Ce.Gateway.Api.Models;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    /// <summary>
    /// Service for generating request reports with caching
    /// </summary>
    public interface IRequestReportService
    {
        /// <summary>
        /// Gets report data for a time range
        /// Uses cache when possible, falls back to aggregates or raw queries as needed
        /// </summary>
        Task<RequestReportDto> GetReportAsync(
            DateTime from,
            DateTime to,
            Granularity granularity,
            ReportFilter filter = null);

        /// <summary>
        /// Invalidates cached report data for a specific time range
        /// Called by the background worker after updating aggregates
        /// </summary>
        void InvalidateCache(DateTime from, DateTime to);
    }
}
