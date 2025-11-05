using System;

namespace Ce.Gateway.Api.Models
{
    public class NodeErrorLogDto
    {
        public DateTime CreatedAtUtc { get; set; }
        public string Route { get; set; }
        public string Node { get; set; }
        public string ErrorMessage { get; set; }
        public long GatewayLatencyMs { get; set; }
        public int? DownstreamStatusCode { get; set; }
        public string RequestBody { get; set; }
    }
}
