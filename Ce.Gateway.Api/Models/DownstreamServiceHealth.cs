using System;
using System.Collections.Generic;

namespace Ce.Gateway.Api.Models
{
    public class DownstreamServiceHealth
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }
        public string Status { get; set; } // Overall status (Healthy, Degraded, Unhealthy)
        public DateTime LastChecked { get; set; }
        public string StatusMessage { get; set; }
        public string TotalDuration { get; set; }
        public Dictionary<string, HealthCheckEntry> Entries { get; set; }

        public string Url => $"{Scheme}://{Host}:{Port}/health";
    }
}
