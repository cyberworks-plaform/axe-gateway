using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MockOcrApi.HealthChecks
{
    public class RandomHealthCheck : IHealthCheck
    {
        private static readonly Random _random = new Random();

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (_random.Next(1,100) == 1)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Mockup ocr service degraded."));
            }
            return Task.FromResult(HealthCheckResult.Healthy("Mockup ocr service healthy."));

        }
    }
}
