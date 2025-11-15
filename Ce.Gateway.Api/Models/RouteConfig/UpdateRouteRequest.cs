using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Request to update route parameters
/// </summary>
public class UpdateRouteRequest
{
    /// <summary>
    /// Route ID to update
    /// </summary>
    [Required]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Downstream path template
    /// </summary>
    public string? DownstreamPathTemplate { get; set; }

    /// <summary>
    /// Downstream scheme (http/https)
    /// </summary>
    public string? DownstreamScheme { get; set; }

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public List<string>? UpstreamHttpMethod { get; set; }

    /// <summary>
    /// Load balancer options
    /// </summary>
    public LoadBalancerOptionsDto? LoadBalancerOptions { get; set; }

    /// <summary>
    /// Quality of Service options
    /// </summary>
    public QoSOptionsDto? QoSOptions { get; set; }

    /// <summary>
    /// Accept any server certificate validator
    /// </summary>
    public bool? DangerousAcceptAnyServerCertificateValidator { get; set; }

    /// <summary>
    /// Priority of this route
    /// </summary>
    public int? Priority { get; set; }
}
