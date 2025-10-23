using System;

namespace Ce.Gateway.Api.Models
{
    public class LogDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string TraceId { get; set; }

        // Upstream information (from client to gateway)
        public string UpstreamHost { get; set; }
        public int? UpstreamPort { get; set; }
        public string UpstreamScheme { get; set; }
        public string UpstreamHttpMethod { get; set; }
        public string UpstreamPath { get; set; }
        public string UpstreamQueryString { get; set; }
        public long? UpstreamRequestSize { get; set; }
        public string UpstreamClientIp { get; set; }

        // Downstream information (from gateway to service)
        public string DownstreamScheme { get; set; }
        public string DownstreamHost { get; set; }
        public int? DownstreamPort { get; set; }
        public string DownstreamPath { get; set; }
        public string DownstreamQueryString { get; set; }
        public long? DownstreamRequestSize { get; set; }
        public long? DownstreamResponseSize { get; set; }
        public int? DownstreamStatusCode { get; set; }

        // Gateway information
        public long GatewayLatencyMs { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }
        public string RequestBody { get; set; }
    }
}
