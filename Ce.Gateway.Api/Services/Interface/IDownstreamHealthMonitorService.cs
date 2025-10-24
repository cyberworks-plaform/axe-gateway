using Ce.Gateway.Api.Models;
using System.Collections.Concurrent;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface IDownstreamHealthMonitorService
    {
        ConcurrentDictionary<string, DownstreamServiceHealth> GetHealthStatus();
    }
}
