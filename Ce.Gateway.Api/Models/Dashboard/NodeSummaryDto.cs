namespace Ce.Gateway.Api.Models.Dashboard
{
    public class NodeSummaryDto
    {
        public string Node { get; set; }
        public long MinLatencyMs { get; set; }
        public long MaxLatencyMs { get; set; }
        public long AvgLatencyMs { get; set; }
        public long TotalRequests { get; set; }
    }
}
