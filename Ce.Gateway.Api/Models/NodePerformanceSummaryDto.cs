namespace Ce.Gateway.Api.Models
{
    public class NodePerformanceSummaryDto
    {
        public string NodeIdentifier { get; set; } // e.g., localhost:10501
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long ErrorRequests { get; set; }
        public double ErrorRate { get; set; } // Percentage
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double AvgLatencyMs { get; set; }
    }
}
