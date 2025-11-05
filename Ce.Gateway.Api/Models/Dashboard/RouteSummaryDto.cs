namespace Ce.Gateway.Api.Models.Dashboard
{
    public class RouteSummaryDto
    {
        public string Route { get; set; }
        public long MinLatencyMs { get; set; }
        public long MaxLatencyMs { get; set; }
        public long AvgLatencyMs { get; set; }
        public long TotalRequests { get; set; }
    }
}
