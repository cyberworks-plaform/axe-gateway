using System;

namespace Ce.Gateway.Api.Models
{
    public class LogDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string TraceId { get; set; }
        public string Route { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string DownstreamNode { get; set; }
        public int StatusCode { get; set; }
        public long LatencyMs { get; set; }
        public string ServiceApi { get; set; }
        public string Client { get; set; }
        public string Error { get; set; }
    }
}
