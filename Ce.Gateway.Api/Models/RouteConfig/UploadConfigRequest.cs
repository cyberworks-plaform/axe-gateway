using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Request to upload configuration file
/// </summary>
public class UploadConfigRequest
{
    /// <summary>
    /// Configuration file content (JSON string)
    /// </summary>
    [Required]
    public string ConfigurationContent { get; set; } = string.Empty;

    /// <summary>
    /// Version information (optional, parsed from filename or provided)
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Description of the change
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User confirmation that they understand the risks
    /// </summary>
    [Required]
    public bool ConfirmRisks { get; set; }
}
