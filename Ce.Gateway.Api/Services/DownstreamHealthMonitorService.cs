using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ocelot.Configuration.File;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public class DownstreamHealthMonitorService : BackgroundService, IDownstreamHealthMonitorService
    {
        private readonly ILogger<DownstreamHealthMonitorService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private ConcurrentDictionary<string, DownstreamServiceHealth> _healthStatus;
        private Timer _timer;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public DownstreamHealthMonitorService(
            ILogger<DownstreamHealthMonitorService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _healthStatus = new ConcurrentDictionary<string, DownstreamServiceHealth>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Downstream Health Monitor Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30)); // Check every 30 seconds

            await Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            if (!await _semaphore.WaitAsync(0))
            {
                _logger.LogInformation("Skipping Downstream Health Monitor Service check as previous one is still running.");
                return;
            }

            try
            {
                _logger.LogInformation("Downstream Health Monitor Service is checking health.");
                await CheckAllDownstreamServicesHealth();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CheckAllDownstreamServicesHealth()
        {
            var currentHealthStatus = new ConcurrentDictionary<string, DownstreamServiceHealth>();
            var downstreamServices = GetUniqueDownstreamServicesFromOcelotConfig();
            var tasks = new List<Task>();

            foreach (var service in downstreamServices)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var isHealthy = false;
                    var statusMessage = "Unknown";
                    try
                    {
                        var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout for health checks
                        var response = await client.GetAsync(service.Url);
                        isHealthy = response.IsSuccessStatusCode;
                        statusMessage = isHealthy ? "Healthy" : $"Unhealthy: {response.StatusCode}";
                    }
                    catch (Exception ex)
                    {
                        statusMessage = $"Unhealthy: {ex.Message}";
                        _logger.LogError(ex, "Error checking health for {Url}", service.Url);
                    }
                    finally
                    {
                        service.IsHealthy = isHealthy;
                        service.LastChecked = DateTime.UtcNow;
                        service.StatusMessage = statusMessage;
                        currentHealthStatus.AddOrUpdate(service.Url, service, (key, oldValue) => service);
                    }
                }));
            }
            await Task.WhenAll(tasks);
            _healthStatus = currentHealthStatus;
        }

        private HashSet<DownstreamServiceHealth> GetUniqueDownstreamServicesFromOcelotConfig()
        {
            var uniqueServices = new HashSet<DownstreamServiceHealth>(new DownstreamServiceHealthComparer());

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var ocelotConfigFile = string.IsNullOrEmpty(env)
                ? "configuration.json"
                : $"configuration.{env}.json";

            var configPath = Path.Combine(AppContext.BaseDirectory, ocelotConfigFile);

            if (!File.Exists(configPath))
            {
                _logger.LogError("Ocelot configuration file not found at {Path}", configPath);
                return uniqueServices;
            }

            var fileContent = File.ReadAllText(configPath);
            var ocelotConfig = JsonConvert.DeserializeObject<FileConfiguration>(fileContent);

            if (ocelotConfig?.Routes == null)
            {
                _logger.LogWarning("Ocelot configuration routes are null or empty.");
                return uniqueServices;
            }

            foreach (var route in ocelotConfig.Routes)
            {
                if (route.DownstreamHostAndPorts != null)
                {
                    foreach (var hostAndPort in route.DownstreamHostAndPorts)
                    {
                        uniqueServices.Add(new DownstreamServiceHealth
                        {
                            Host = hostAndPort.Host,
                            Port = hostAndPort.Port,
                            Scheme = route.DownstreamScheme ?? "http" // Default to http if not specified
                        });
                    }
                }
            }
            return uniqueServices;
        }

        public ConcurrentDictionary<string, DownstreamServiceHealth> GetHealthStatus()
        {
            return _healthStatus;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Downstream Health Monitor Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            await base.StopAsync(stoppingToken);
        }

        private class DownstreamServiceHealthComparer : IEqualityComparer<DownstreamServiceHealth>
        {
            public bool Equals(DownstreamServiceHealth x, DownstreamServiceHealth y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Host == y.Host && x.Port == y.Port && x.Scheme == y.Scheme;
            }

            public int GetHashCode(DownstreamServiceHealth obj)
            {
                return HashCode.Combine(obj.Host, obj.Port, obj.Scheme);
            }
        }
    }
}
