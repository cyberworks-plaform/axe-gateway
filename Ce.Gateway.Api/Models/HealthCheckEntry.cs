using System.Collections.Generic;

namespace Ce.Gateway.Api.Models
{
    public class HealthCheckEntry
    {
        public Dictionary<string, object> Data { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public string Status { get; set; }
        public List<string> Tags { get; set; }
    }
}
