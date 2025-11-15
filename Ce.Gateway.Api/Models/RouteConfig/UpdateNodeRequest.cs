using System;
using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Request to update a node in a route
/// </summary>
public class UpdateNodeRequest
{
    /// <summary>
    /// Route ID
    /// </summary>
    [Required]
    public string RouteId { get; set; } = string.Empty;

    /// <summary>
    /// Original host address
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9.-]+$", ErrorMessage = "Host must contain only alphanumeric characters, dots, and hyphens")]
    public string OldHost { get; set; } = string.Empty;

    /// <summary>
    /// Original port number
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int OldPort { get; set; }

    /// <summary>
    /// New host address
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9.-]+$", ErrorMessage = "Host must contain only alphanumeric characters, dots, and hyphens")]
    public string NewHost { get; set; } = string.Empty;

    /// <summary>
    /// New port number
    /// </summary>
    [Required]
    [Range(1, 65535)]
    public int NewPort { get; set; }
}
