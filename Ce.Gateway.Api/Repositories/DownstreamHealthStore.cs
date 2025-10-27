using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.Extensions.Logging;

namespace Ce.Gateway.Api.Repositories
{
    public class DownstreamHealthStore : IDownstreamHealthStore
    {
        private readonly ConcurrentDictionary<string, DownstreamServiceHealth> _healthStore;
        private readonly ILogger<DownstreamHealthStore> _logger;

        public DownstreamHealthStore(ILogger<DownstreamHealthStore> logger)
        {
            _healthStore = new ConcurrentDictionary<string, DownstreamServiceHealth>();
            _logger = logger;
        }

        public Task<IEnumerable<DownstreamServiceHealth>> GetAllHealthAsync()
        {
            _logger.LogDebug("Retrieving all downstream service healths.");
            return Task.FromResult(_healthStore.Values.AsEnumerable());
        }

        public Task<DownstreamServiceHealth> GetHealthAsync(string host, int port)
        {
            var key = GenerateKey(host, port);
            _logger.LogDebug($"Retrieving health for service: {key}");
            _healthStore.TryGetValue(key, out var health);
            return Task.FromResult(health);
        }

        public Task UpdateHealthAsync(DownstreamServiceHealth health)
        {
            var key = GenerateKey(health.Host, health.Port);
            _healthStore.AddOrUpdate(key, health,
                (k, existingVal) =>
                {
                    _logger.LogDebug($"Updating health for service: {key}. Old status: {existingVal.Status}, New status: {health.Status}");
                    return health;
                });
            return Task.CompletedTask;
        }

        public async Task UpdateHealthAsync(IEnumerable<DownstreamServiceHealth> healths)
        {
            foreach (var health in healths)
            {
                await UpdateHealthAsync(health);
            }
        }

        private string GenerateKey(string host, int port)
        {
            return $"{host}:{port}";
        }
    }
}
