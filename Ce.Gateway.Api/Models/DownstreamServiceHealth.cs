using System;

namespace Ce.Gateway.Api.Models
{
    public class DownstreamServiceHealth
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Scheme { get; set; }
        public bool IsHealthy { get; set; }
        public DateTime LastChecked { get; set; }
        public string StatusMessage { get; set; }

        public string Url => $"{Scheme}://{Host}:{Port}/health";
    }
}
