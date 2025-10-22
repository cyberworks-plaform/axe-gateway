
using System;

namespace Ce.Gateway.Api.Entities
{
    public class OcrGatewayLogEntry
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
        public long RequestSize { get; set; }
        public long ResponseSize { get; set; }
        public string Error { get; set; }
    }
}
