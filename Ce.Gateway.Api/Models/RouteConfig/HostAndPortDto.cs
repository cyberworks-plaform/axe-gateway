using System;
namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Represents a downstream host and port node
/// </summary>
public class HostAndPortDto
{
    /// <summary>
    /// Host address (domain or IP)
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port number
    /// </summary>
    public int Port { get; set; }
}
