using System;

namespace Ce.Gateway.Api.Models.Dashboard
{
    public class ErrorLogDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string UpstreamPath { get; set; }
        public string UpstreamHost { get; set; }
        public string ErrorMessage { get; set; }
        public long GatewayLatencyMs { get; set; }
        public int? DownstreamStatusCode { get; set; }
        public string RequestBody { get; set; }
    }
}
