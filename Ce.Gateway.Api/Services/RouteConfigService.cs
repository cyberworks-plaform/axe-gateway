using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.RouteConfig;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ce.Gateway.Api.Services;

/// <summary>
/// Service for managing Ocelot route configurations
/// </summary>
public class RouteConfigService : IRouteConfigService
{
    private readonly ILogger<RouteConfigService> _logger;
    private readonly GatewayDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly string _configFilePath;
    private readonly string _backupDirectory;
    private static readonly SemaphoreSlim _configLock = new(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null, // Use PascalCase to match Ocelot configuration format
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true // Allow case-insensitive property matching
    };

    public RouteConfigService(
        ILogger<RouteConfigService> logger,
        GatewayDbContext context,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
        _environment = environment;

        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        _configFilePath = string.IsNullOrEmpty(env) || env == "Production"
            ? Path.Combine(environment.ContentRootPath, "configuration.json")
            : Path.Combine(environment.ContentRootPath, $"configuration.{env}.json");

        _backupDirectory = Path.Combine(environment.ContentRootPath, "data", "config-backups");
        Directory.CreateDirectory(_backupDirectory);
    }

    public async Task<List<RouteDto>> GetAllRoutesAsync()
    {
        try
        {
            var config = await ReadConfigurationAsync();
            return config.Routes.Select(MapToRouteDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading route configuration");
            throw;
        }
    }

    public async Task<RouteDto?> GetRouteByIdAsync(string routeId)
    {
        var routes = await GetAllRoutesAsync();
        return routes.FirstOrDefault(r => r.RouteId == routeId);
    }

    public async Task<bool> AddNodeToRoutesAsync(AddNodeRequest request, string userName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await _configLock.WaitAsync();
        try
        {
            var config = await ReadConfigurationAsync();
            var modified = false;

            foreach (var routeId in request.RouteIds)
            {
                var route = config.Routes.FirstOrDefault(r => GenerateRouteId(r.UpstreamPathTemplate) == routeId);
                if (route != null)
                {
                    route.DownstreamHostAndPorts ??= new List<OcelotHostAndPort>();
                    
                    // Check if node already exists
                    if (!route.DownstreamHostAndPorts.Any(n => n.Host == request.Host && n.Port == request.Port))
                    {
                        route.DownstreamHostAndPorts.Add(new OcelotHostAndPort
                        {
                            Host = request.Host,
                            Port = request.Port
                        });
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                await BackupAndSaveConfigurationAsync(config, $"Added node {request.Host}:{request.Port} to {request.RouteIds.Count} route(s)", userName);
                return true;
            }

            return false;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task<bool> UpdateNodeInRouteAsync(UpdateNodeRequest request, string userName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await _configLock.WaitAsync();
        try
        {
            var config = await ReadConfigurationAsync();
            var route = config.Routes.FirstOrDefault(r => GenerateRouteId(r.UpstreamPathTemplate) == request.RouteId);

            if (route?.DownstreamHostAndPorts == null)
                return false;

            var node = route.DownstreamHostAndPorts.FirstOrDefault(n =>
                n.Host == request.OldHost && n.Port == request.OldPort);

            if (node == null)
                return false;

            node.Host = request.NewHost;
            node.Port = request.NewPort;

            await BackupAndSaveConfigurationAsync(config,
                $"Updated node from {request.OldHost}:{request.OldPort} to {request.NewHost}:{request.NewPort} in route {request.RouteId}",
                userName);

            return true;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task<bool> DeleteNodeFromRoutesAsync(DeleteNodeRequest request, string userName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await _configLock.WaitAsync();
        try
        {
            var config = await ReadConfigurationAsync();
            var modified = false;

            foreach (var routeId in request.RouteIds)
            {
                var route = config.Routes.FirstOrDefault(r => GenerateRouteId(r.UpstreamPathTemplate) == routeId);
                if (route?.DownstreamHostAndPorts != null)
                {
                    var nodeToRemove = route.DownstreamHostAndPorts.FirstOrDefault(n =>
                        n.Host == request.Host && n.Port == request.Port);

                    if (nodeToRemove != null)
                    {
                        route.DownstreamHostAndPorts.Remove(nodeToRemove);
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                await BackupAndSaveConfigurationAsync(config,
                    $"Deleted node {request.Host}:{request.Port} from {request.RouteIds.Count} route(s)",
                    userName);
                return true;
            }

            return false;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task<bool> UpdateRouteAsync(UpdateRouteRequest request, string userName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await _configLock.WaitAsync();
        try
        {
            var config = await ReadConfigurationAsync();
            var route = config.Routes.FirstOrDefault(r => GenerateRouteId(r.UpstreamPathTemplate) == request.RouteId);

            if (route == null)
                return false;

            // Update only non-null properties
            if (request.DownstreamPathTemplate != null)
                route.DownstreamPathTemplate = request.DownstreamPathTemplate;

            if (request.DownstreamScheme != null)
                route.DownstreamScheme = request.DownstreamScheme;

            if (request.UpstreamHttpMethod != null)
                route.UpstreamHttpMethod = request.UpstreamHttpMethod;

            if (request.LoadBalancerOptions != null)
            {
                route.LoadBalancerOptions = new OcelotLoadBalancerOptions
                {
                    Type = request.LoadBalancerOptions.Type,
                    Key = request.LoadBalancerOptions.Key,
                    Expiry = request.LoadBalancerOptions.Expiry
                };
            }

            if (request.QoSOptions != null)
            {
                route.QoSOptions = new OcelotQoSOptions
                {
                    TimeoutValue = request.QoSOptions.TimeoutValue,
                    ExceptionsAllowedBeforeBreaking = request.QoSOptions.ExceptionsAllowedBeforeBreaking,
                    DurationOfBreak = request.QoSOptions.DurationOfBreak
                };
            }

            if (request.DangerousAcceptAnyServerCertificateValidator.HasValue)
                route.DangerousAcceptAnyServerCertificateValidator = request.DangerousAcceptAnyServerCertificateValidator;

            if (request.Priority.HasValue)
                route.Priority = request.Priority;

            await BackupAndSaveConfigurationAsync(config, $"Updated route {request.RouteId}", userName);
            return true;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public async Task<List<ConfigurationHistoryDto>> GetConfigurationHistoryAsync(int limit = 50)
    {
        var histories = await _context.ConfigurationHistories
            .OrderByDescending(h => h.Timestamp)
            .Take(limit)
            .ToListAsync();

        return histories.Select(h => new ConfigurationHistoryDto
        {
            Id = h.Id,
            Timestamp = h.Timestamp,
            ChangedBy = h.ChangedBy,
            Description = h.Description,
            BackupFilePath = Path.Combine(_backupDirectory, h.BackupFileName),
            IsActive = h.IsActive
        }).ToList();
    }

    public async Task<bool> RollbackConfigurationAsync(string historyId, string userName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(historyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        await _configLock.WaitAsync();
        try
        {
            var history = await _context.ConfigurationHistories.FindAsync(historyId);
            if (history == null)
            {
                _logger.LogWarning("Configuration history {HistoryId} not found", historyId);
                return false;
            }

            var backupFilePath = Path.Combine(_backupDirectory, history.BackupFileName);
            if (!File.Exists(backupFilePath))
            {
                _logger.LogError("Backup file not found: {BackupFilePath}", backupFilePath);
                return false;
            }

            // Read the backup configuration
            var backupContent = await File.ReadAllTextAsync(backupFilePath);
            var backupConfig = JsonSerializer.Deserialize<OcelotConfiguration>(backupContent, _jsonOptions);

            if (backupConfig == null)
            {
                _logger.LogError("Failed to deserialize backup configuration");
                return false;
            }

            // Create backup of current config before rollback
            var currentConfig = await ReadConfigurationAsync();
            await CreateBackupAsync(currentConfig, $"Pre-rollback backup (rolling back to {history.Timestamp:yyyy-MM-dd HH:mm:ss})", userName, false);

            // Write the backup config as the current config
            var json = JsonSerializer.Serialize(backupConfig, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json);

            // Update history records
            await _context.ConfigurationHistories
                .Where(h => h.IsActive)
                .ExecuteUpdateAsync(h => h.SetProperty(x => x.IsActive, false));

            history.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Configuration rolled back to {HistoryId} by {UserName}", historyId, userName);
            return true;
        }
        finally
        {
            _configLock.Release();
        }
    }

    public Task<bool> ReloadConfigurationAsync()
    {
        // Ocelot supports hot reload when configuration file changes
        // The file watcher built into IConfiguration will automatically detect changes
        _logger.LogInformation("Configuration reload triggered - Ocelot will automatically reload");
        return Task.FromResult(true);
    }

    #region Private Helper Methods

    private async Task<OcelotConfiguration> ReadConfigurationAsync()
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogWarning("Configuration file not found: {ConfigFilePath}", _configFilePath);
            return new OcelotConfiguration();
        }

        var json = await File.ReadAllTextAsync(_configFilePath);
        var config = JsonSerializer.Deserialize<OcelotConfiguration>(json, _jsonOptions);

        return config ?? new OcelotConfiguration();
    }

    private async Task BackupAndSaveConfigurationAsync(OcelotConfiguration config, string description, string userName)
    {
        // Create backup
        await CreateBackupAsync(config, description, userName, true);

        // Save configuration
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(_configFilePath, json);

        _logger.LogInformation("Configuration updated: {Description} by {UserName}", description, userName);
    }

    private async Task CreateBackupAsync(OcelotConfiguration config, string description, string userName, bool setAsActive)
    {
        var timestamp = DateTime.UtcNow;
        var backupFileName = $"config-backup-{timestamp:yyyyMMdd-HHmmss}.json";
        var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

        // Save backup file
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(backupFilePath, json);

        // Set all existing configurations as inactive if this is the new active one
        if (setAsActive)
        {
            await _context.ConfigurationHistories
                .Where(h => h.IsActive)
                .ExecuteUpdateAsync(h => h.SetProperty(x => x.IsActive, false));
        }

        // Save history record
        var history = new ConfigurationHistory
        {
            Timestamp = timestamp,
            ChangedBy = userName,
            Description = description,
            BackupFileName = backupFileName,
            IsActive = setAsActive
        };

        _context.ConfigurationHistories.Add(history);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Configuration backup created: {BackupFileName}", backupFileName);
    }

    private static RouteDto MapToRouteDto(OcelotRoute route)
    {
        return new RouteDto
        {
            RouteId = GenerateRouteId(route.UpstreamPathTemplate),
            DownstreamPathTemplate = route.DownstreamPathTemplate ?? string.Empty,
            DownstreamScheme = route.DownstreamScheme ?? string.Empty,
            DownstreamHostAndPorts = route.DownstreamHostAndPorts?.Select(h => new HostAndPortDto
            {
                Host = h.Host ?? string.Empty,
                Port = h.Port
            }).ToList() ?? new List<HostAndPortDto>(),
            UpstreamPathTemplate = route.UpstreamPathTemplate ?? string.Empty,
            UpstreamHttpMethod = route.UpstreamHttpMethod ?? new List<string>(),
            LoadBalancerOptions = route.LoadBalancerOptions != null ? new LoadBalancerOptionsDto
            {
                Type = route.LoadBalancerOptions.Type,
                Key = route.LoadBalancerOptions.Key,
                Expiry = route.LoadBalancerOptions.Expiry
            } : null,
            QoSOptions = route.QoSOptions != null ? new QoSOptionsDto
            {
                TimeoutValue = route.QoSOptions.TimeoutValue,
                ExceptionsAllowedBeforeBreaking = route.QoSOptions.ExceptionsAllowedBeforeBreaking,
                DurationOfBreak = route.QoSOptions.DurationOfBreak
            } : null,
            DangerousAcceptAnyServerCertificateValidator = route.DangerousAcceptAnyServerCertificateValidator,
            Priority = route.Priority,
            RequestIdKey = route.RequestIdKey
        };
    }

    private static string GenerateRouteId(string? upstreamPathTemplate)
    {
        if (string.IsNullOrEmpty(upstreamPathTemplate))
            return Guid.NewGuid().ToString();

        // Create a stable ID from the upstream path
        return Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(upstreamPathTemplate))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    #endregion
}
