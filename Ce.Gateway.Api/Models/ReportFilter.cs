using System.Linq;

namespace Ce.Gateway.Api.Models
{
    /// <summary>
    /// Filter options for request report queries
    /// </summary>
    public class ReportFilter
    {
        public string UpstreamPath { get; set; }
        public string DownstreamHost { get; set; }
        public string UpstreamClientIp { get; set; }
        
        /// <summary>
        /// Gets a normalized string representation of the filter for cache key generation
        /// </summary>
        public string GetNormalizedKey()
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(UpstreamPath) ? "" : $"path:{UpstreamPath}",
                string.IsNullOrWhiteSpace(DownstreamHost) ? "" : $"host:{DownstreamHost}",
                string.IsNullOrWhiteSpace(UpstreamClientIp) ? "" : $"ip:{UpstreamClientIp}"
            };
            
            return string.Join("_", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
        
        /// <summary>
        /// Checks if the filter has any active criteria
        /// </summary>
        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(UpstreamPath) 
                && string.IsNullOrWhiteSpace(DownstreamHost) 
                && string.IsNullOrWhiteSpace(UpstreamClientIp);
        }
    }
}
