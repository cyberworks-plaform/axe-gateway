using System;

namespace Ce.Gateway.Api.Models
{
    public class LogFilter
    {

        public string DownstreamHost { get; set; }
        public string UpstreamClientIp { get; set; }
        public int? DownstreamStatusCode { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
