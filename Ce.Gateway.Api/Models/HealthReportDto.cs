using System.Collections.Generic;
using Newtonsoft.Json;

namespace Ce.Gateway.Api.Models
{
    public class HealthReportDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("totalDuration")]
        public string TotalDuration { get; set; }

        [JsonProperty("entries")]
        public Dictionary<string, HealthCheckEntry> Entries { get; set; }
    }
}
