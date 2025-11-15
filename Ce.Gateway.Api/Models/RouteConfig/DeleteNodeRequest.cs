using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Request to delete a node from one or more routes
/// </summary>
public class DeleteNodeRequest
{
    /// <summary>
    /// Route IDs to delete the node from
    /// </summary>
    [Required]
    public List<string> RouteIds { get; set; } = new();

    /// <summary>
    /// Host address to delete
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9.-]+$", ErrorMessage = "Host must contain only alphanumeric characters, dots, and hyphens")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Port number to delete
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int Port { get; set; }
}
