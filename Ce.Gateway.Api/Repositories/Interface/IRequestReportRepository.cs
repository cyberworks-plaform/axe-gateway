using Ce.Gateway.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories.Interface
{
    /// <summary>
    /// Repository for request report data access
    /// Provides both aggregated (fast) and raw (flexible) query methods
    /// </summary>
    public interface IRequestReportRepository
    {
        /// <summary>
        /// Gets pre-aggregated counts from the RequestReportAggregates table
        /// This is fast but only works when no additional filters are applied
        /// </summary>
        Task<RequestReportDto> GetAggregatedCountsAsync(
            DateTime from, 
            DateTime to, 
            Granularity granularity, 
            ReportFilter filter);

        /// <summary>
        /// Gets counts by querying the raw OcrGatewayLogEntries table
        /// This is slower but supports all filters and real-time data
        /// </summary>
        Task<RequestReportDto> GetRawCountsAsync(
            DateTime from, 
            DateTime to, 
            Granularity granularity, 
            ReportFilter filter);

        /// <summary>
        /// Upserts aggregated data for a specific period
        /// Used by the background worker to update materialized view
        /// </summary>
        Task UpsertAggregatesAsync(
            DateTime periodStart, 
            Granularity granularity, 
            Dictionary<int, long> statusCounts);

        /// <summary>
        /// Gets the last updated timestamp for aggregates in a date range
        /// Used to determine if cached data is stale
        /// </summary>
        Task<DateTime?> GetAggregatesLastUpdatedAsync(
            DateTime from, 
            DateTime to, 
            Granularity granularity);
    }
}
