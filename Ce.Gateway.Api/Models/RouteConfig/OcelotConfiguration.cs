using System;
namespace Ce.Gateway.Api.Models.RouteConfig;
using System.Collections.Generic;

/// <summary>
/// Represents the complete Ocelot configuration structure
/// </summary>
public class OcelotConfiguration
{
    /// <summary>
    /// List of routes
    /// </summary>
    public List<OcelotRoute> Routes { get; set; } = new();

    /// <summary>
    /// Global configuration options
    /// </summary>
    public OcelotGlobalConfiguration? GlobalConfiguration { get; set; }
}

/// <summary>
/// Represents a single Ocelot route
/// </summary>
public class OcelotRoute
{
    public string? DownstreamPathTemplate { get; set; }
    public string? DownstreamScheme { get; set; }
    public List<OcelotHostAndPort>? DownstreamHostAndPorts { get; set; }
    public string? UpstreamPathTemplate { get; set; }
    public List<string>? UpstreamHttpMethod { get; set; }
    public OcelotLoadBalancerOptions? LoadBalancerOptions { get; set; }
    public OcelotQoSOptions? QoSOptions { get; set; }
    public bool? DangerousAcceptAnyServerCertificateValidator { get; set; }
    public int? Priority { get; set; }
    public string? RequestIdKey { get; set; }
}

/// <summary>
/// Represents a host and port pair
/// </summary>
public class OcelotHostAndPort
{
    public string? Host { get; set; }
    public int Port { get; set; }
}

/// <summary>
/// Load balancer options
/// </summary>
public class OcelotLoadBalancerOptions
{
    public string? Type { get; set; }
    public string? Key { get; set; }
    public int? Expiry { get; set; }
}

/// <summary>
/// Quality of Service options
/// </summary>
public class OcelotQoSOptions
{
    public int? TimeoutValue { get; set; }
    public int? ExceptionsAllowedBeforeBreaking { get; set; }
    public int? DurationOfBreak { get; set; }
}

/// <summary>
/// Global configuration options
/// </summary>
public class OcelotGlobalConfiguration
{
    public string? BaseUrl { get; set; }
    public string? RequestIdKey { get; set; }
}
