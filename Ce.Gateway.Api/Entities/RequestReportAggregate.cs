using System;

namespace Ce.Gateway.Api.Entities
{
    /// <summary>
    /// Entity for storing pre-aggregated request report data
    /// This acts as a materialized view for faster report queries
    /// </summary>
    public class RequestReportAggregate
    {
        /// <summary>
        /// The start of the time period (date only for day/month granularity)
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Granularity of the aggregation: 'day' or 'month'
        /// </summary>
        public string Granularity { get; set; }

        /// <summary>
        /// HTTP status code category: 2 (2xx), 4 (4xx), 5 (5xx), 0 (other)
        /// </summary>
        public int StatusCategory { get; set; }

        /// <summary>
        /// Count of requests for this period, granularity, and status category
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// Timestamp of when this aggregate was last updated
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }
    }
}
