using System;

namespace Ce.Gateway.Api.Models
{
    public class LogFilter
    {
        public string Route { get; set; }
        public string Node { get; set; }
        public int? StatusCode { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
