using System.Collections.Generic;
using System.Threading.Tasks;
using Ce.Gateway.Api.Models.RouteConfig;

namespace Ce.Gateway.Api.Services.Interface;

/// <summary>
/// Service for managing Ocelot route configurations
/// </summary>
public interface IRouteConfigService
{
    /// <summary>
    /// Get all routes from the configuration
    /// </summary>
    Task<List<RouteDto>> GetAllRoutesAsync();

    /// <summary>
    /// Get a specific route by ID
    /// </summary>
    Task<RouteDto?> GetRouteByIdAsync(string routeId);

    /// <summary>
    /// Add a node to one or more routes
    /// </summary>
    Task<bool> AddNodeToRoutesAsync(AddNodeRequest request, string userName);

    /// <summary>
    /// Update a node in a route
    /// </summary>
    Task<bool> UpdateNodeInRouteAsync(UpdateNodeRequest request, string userName);

    /// <summary>
    /// Delete a node from one or more routes
    /// </summary>
    Task<bool> DeleteNodeFromRoutesAsync(DeleteNodeRequest request, string userName);

    /// <summary>
    /// Update route parameters
    /// </summary>
    Task<bool> UpdateRouteAsync(UpdateRouteRequest request, string userName);

    /// <summary>
    /// Create a new route
    /// </summary>
    Task<bool> CreateRouteAsync(RouteDto route, string userName);

    /// <summary>
    /// Get configuration history
    /// </summary>
    Task<List<ConfigurationHistoryDto>> GetConfigurationHistoryAsync(int limit = 50);

    /// <summary>
    /// Rollback to a previous configuration
    /// </summary>
    Task<bool> RollbackConfigurationAsync(string historyId, string userName);

    /// <summary>
    /// Reload Ocelot configuration (triggers hot reload)
    /// </summary>
    Task<bool> ReloadConfigurationAsync();
}
