using System;
namespace Ce.Gateway.Api.Models.RouteConfig;
using System.Collections.Generic;

/// <summary>
/// Represents an Ocelot route configuration
/// </summary>
public class RouteDto
{
    /// <summary>
    /// Unique identifier for the route (generated from UpstreamPathTemplate)
    /// </summary>
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Downstream path template
    /// </summary>
    public string DownstreamPathTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Downstream scheme (http/https)
    /// </summary>
    public string DownstreamScheme { get; set; } = string.Empty;

    /// <summary>
    /// List of downstream host and port nodes
    /// </summary>
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();

    /// <summary>
    /// Upstream path template
    /// </summary>
    public string UpstreamPathTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public List<string> UpstreamHttpMethod { get; set; } = new();

    /// <summary>
    /// Load balancer options
    /// </summary>
    public LoadBalancerOptionsDto? LoadBalancerOptions { get; set; }

    /// <summary>
    /// Quality of Service options
    /// </summary>
    public QoSOptionsDto? QoSOptions { get; set; }

    /// <summary>
    /// Accept any server certificate validator (for development)
    /// </summary>
    public bool? DangerousAcceptAnyServerCertificateValidator { get; set; }

    /// <summary>
    /// Priority of this route (lower values = higher priority)
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Request ID key
    /// </summary>
    public string? RequestIdKey { get; set; }
}
