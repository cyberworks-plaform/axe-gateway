using Ce.Gateway.Api.Repositories.Interface;
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
    public class DownstreamHealthMonitorService : BackgroundService
    {
        private readonly ILogger<DownstreamHealthMonitorService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IDownstreamHealthStore _downstreamHealthStore;
        private readonly int _evaluationTimeInSeconds;
        private Timer _timer;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public DownstreamHealthMonitorService(
            ILogger<DownstreamHealthMonitorService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IDownstreamHealthStore downstreamHealthStore)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _downstreamHealthStore = downstreamHealthStore;
            _evaluationTimeInSeconds = _configuration.GetValue<int?>("HealthChecksUI:EvaluationTimeInSeconds") ?? 60;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Downstream Health Monitor Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(_evaluationTimeInSeconds));

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
            var downstreamServices = GetUniqueDownstreamServicesFromOcelotConfig();
            var tasks = new List<Task>();

            foreach (var service in downstreamServices)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5); // 5 second timeout for health checks

                    service.LastChecked = DateTime.UtcNow; // Set last checked time regardless of outcome

                    HttpResponseMessage response = null;
                    string jsonResponse = null;
                    HealthReportDto healthReport = null;

                    try
                    {
                        response = await client.GetAsync(service.Url);
                        
                        // Always try to read content, even for non-success status codes
                        jsonResponse = await response.Content.ReadAsStringAsync();

                        // Attempt to deserialize JSON content
                        if (!string.IsNullOrEmpty(jsonResponse))
                        {
                            try
                            {
                                healthReport = JsonConvert.DeserializeObject<HealthReportDto>(jsonResponse);
                            }
                            catch (JsonSerializationException jsonEx)
                            {
                                _logger.LogWarning(jsonEx, "Could not deserialize health response for {Url}. Content: {Content}", service.Url, jsonResponse);
                                // If deserialization fails, healthReport remains null
                            }
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            // Successful response, use parsed health report or default to Healthy
                            service.Status = healthReport?.Status ?? "Healthy";
                            service.TotalDuration = healthReport?.TotalDuration;
                            service.Entries = healthReport?.Entries;
                            service.StatusMessage = $"Healthy: {service.Status}";
                        }
                        else
                        {
                            // Non-success status code (4xx, 5xx)
                            service.Status = healthReport?.Status ?? "Unhealthy"; // Use parsed status if available, else Unhealthy
                            service.TotalDuration = healthReport?.TotalDuration;
                            service.Entries = healthReport?.Entries;
                            service.StatusMessage = $"Unhealthy: HTTP {response.StatusCode} - {response.ReasonPhrase}";
                            _logger.LogWarning("Health check for {Url} returned non-success status {StatusCode}. Message: {Message}", service.Url, response.StatusCode, service.StatusMessage);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        service.Status = "Unhealthy";
                        service.StatusMessage = $"Unhealthy: Could not reach service. {ex.Message}";
                        _logger.LogError(ex, "Error checking health for {Url}: {Message}", service.Url, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        service.Status = "Unhealthy";
                        service.StatusMessage = $"Unhealthy: An unexpected error occurred. {ex.Message}";
                        _logger.LogError(ex, "Unexpected error checking health for {Url}: {Message}", service.Url, ex.Message);
                    }
                    finally
                    {
                        await _downstreamHealthStore.UpdateHealthAsync(service);
                    }
                }));
            }
            await Task.WhenAll(tasks);
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
