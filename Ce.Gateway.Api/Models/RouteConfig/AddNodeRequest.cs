using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Request to add a node to one or more routes
/// </summary>
public class AddNodeRequest
{
    /// <summary>
    /// Route IDs to add the node to
    /// </summary>
    [Required]
    public List<string> RouteIds { get; set; } = new();

    /// <summary>
    /// Host address to add
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port number to add
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }
}
