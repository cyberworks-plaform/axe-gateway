namespace Ce.Gateway.Api.Models.Dashboard
{
    public class DashboardOverviewAggregateDto
    {
        public long TotalRequests { get; set; }
        public long ErrorRequests { get; set; }
        public long TotalLatencyMs { get; set; }
    }
}
