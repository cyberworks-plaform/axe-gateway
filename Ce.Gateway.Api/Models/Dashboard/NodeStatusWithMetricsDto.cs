using System;
using System.Collections.Generic;

namespace Ce.Gateway.Api.Models.Dashboard
{
    public class NodeStatusWithMetricsDto
    {
        public string Node { get; set; }
        public bool IsHealthy { get; set; }
        public long MinLatencyMs { get; set; }
        public long MaxLatencyMs { get; set; }
        public long AvgLatencyMs { get; set; }
        public long TotalRequests { get; set; }
        public string Status { get; set; }
        public DateTime? LastChecked { get; set; }
        public string StatusMessage { get; set; }
        public string TotalDuration { get; set; }
        public Dictionary<string, HealthCheckEntryDto> Entries { get; set; }
    }

    public class HealthCheckEntryDto
    {
        public string Status { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
    }
}
