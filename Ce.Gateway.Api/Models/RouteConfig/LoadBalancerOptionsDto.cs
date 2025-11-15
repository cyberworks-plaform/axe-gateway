using System;
namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Load balancer configuration options
/// </summary>
public class LoadBalancerOptionsDto
{
    /// <summary>
    /// Load balancer type (e.g., "LeastConnection", "RoundRobin", "NoLoadBalancer")
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Key for load balancer
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Expiry time in milliseconds
    /// </summary>
    public int? Expiry { get; set; }
}
