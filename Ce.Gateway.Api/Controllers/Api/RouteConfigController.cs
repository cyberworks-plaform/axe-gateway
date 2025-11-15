using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Models.RouteConfig;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// API Controller for managing route configurations
    /// Note: CSRF protection via antiforgery tokens should be implemented for production
    /// Current implementation relies on Authorization header validation
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [ApiController]
    [Route("api/routes")]
    public class RouteConfigController : ControllerBase
    {
        private readonly IRouteConfigService _routeConfigService;
        private readonly ILogger<RouteConfigController> _logger;

        public RouteConfigController(
            IRouteConfigService routeConfigService,
            ILogger<RouteConfigController> logger)
        {
            _routeConfigService = routeConfigService;
            _logger = logger;
        }

        /// <summary>
        /// Get all routes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<RouteDto>>>> GetAllRoutes()
        {
            try
            {
                var routes = await _routeConfigService.GetAllRoutesAsync();
                return Ok(new ApiResponse<List<RouteDto>>
                {
                    Success = true,
                    Data = routes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all routes");
                return StatusCode(500, new ApiResponse<List<RouteDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve routes"
                });
            }
        }

        /// <summary>
        /// Get a specific route by ID
        /// </summary>
        [HttpGet("{routeId}")]
        public async Task<ActionResult<ApiResponse<RouteDto>>> GetRoute(string routeId)
        {
            try
            {
                var route = await _routeConfigService.GetRouteByIdAsync(routeId);
                if (route == null)
                {
                    return NotFound(new ApiResponse<RouteDto>
                    {
                        Success = false,
                        Message = "Route not found"
                    });
                }

                return Ok(new ApiResponse<RouteDto>
                {
                    Success = true,
                    Data = route
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route {RouteId}", routeId);
                return StatusCode(500, new ApiResponse<RouteDto>
                {
                    Success = false,
                    Message = "Failed to retrieve route"
                });
            }
        }

        /// <summary>
        /// Add a node to one or more routes
        /// </summary>
        [HttpPost("nodes")]
        public async Task<ActionResult<ApiResponse<bool>>> AddNode([FromBody] AddNodeRequest request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.AddNodeToRoutesAsync(request, userName);

                if (!result)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to add node. Node may already exist or routes not found."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Node {request.Host}:{request.Port} added successfully to {request.RouteIds.Count} route(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding node");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to add node"
                });
            }
        }

        /// <summary>
        /// Update a node in a route
        /// </summary>
        [HttpPut("nodes")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateNode([FromBody] UpdateNodeRequest request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.UpdateNodeInRouteAsync(request, userName);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Node or route not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Node updated successfully from {request.OldHost}:{request.OldPort} to {request.NewHost}:{request.NewPort}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating node");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update node"
                });
            }
        }

        /// <summary>
        /// Delete a node from one or more routes
        /// </summary>
        [HttpDelete("nodes")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteNode([FromBody] DeleteNodeRequest request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.DeleteNodeFromRoutesAsync(request, userName);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Node or route not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Node {request.Host}:{request.Port} deleted successfully from {request.RouteIds.Count} route(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting node");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to delete node"
                });
            }
        }

        /// <summary>
        /// Update route parameters
        /// </summary>
        [HttpPut("{routeId}")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateRoute(string routeId, [FromBody] UpdateRouteRequest request)
        {
            if (routeId != request.RouteId)
            {
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Route ID mismatch"
                });
            }

            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.UpdateRouteAsync(request, userName);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Route not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Route updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating route {RouteId}", routeId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to update route"
                });
            }
        }

        /// <summary>
        /// Create a new route
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<bool>>> CreateRoute([FromBody] RouteDto request)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.CreateRouteAsync(request, userName);

                if (!result)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Failed to create route. Route may already exist."
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Route '{request.UpstreamPathTemplate}' created successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid route data");
                return BadRequest(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Route creation conflict");
                return Conflict(new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating route");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to create route"
                });
            }
        }

        /// <summary>
        /// Get configuration history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<ConfigurationHistoryDto>>>> GetHistory([FromQuery] int limit = 50)
        {
            try
            {
                var history = await _routeConfigService.GetConfigurationHistoryAsync(limit);
                return Ok(new ApiResponse<List<ConfigurationHistoryDto>>
                {
                    Success = true,
                    Data = history
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration history");
                return StatusCode(500, new ApiResponse<List<ConfigurationHistoryDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve configuration history"
                });
            }
        }

        /// <summary>
        /// Rollback to a previous configuration
        /// </summary>
        [HttpPost("rollback/{historyId}")]
        public async Task<ActionResult<ApiResponse<bool>>> Rollback(string historyId)
        {
            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                var result = await _routeConfigService.RollbackConfigurationAsync(historyId, userName);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Configuration history not found or backup file missing"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Configuration rolled back successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back configuration to {HistoryId}", historyId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to rollback configuration"
                });
            }
        }

        /// <summary>
        /// Reload configuration (triggers hot reload)
        /// </summary>
        [HttpPost("reload")]
        public async Task<ActionResult<ApiResponse<bool>>> ReloadConfiguration()
        {
            try
            {
                await _routeConfigService.ReloadConfigurationAsync();
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = "Configuration reload triggered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Failed to reload configuration"
                });
            }
        }
    }
}
