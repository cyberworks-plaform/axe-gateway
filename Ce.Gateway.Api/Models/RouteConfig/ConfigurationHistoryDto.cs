using System;
namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Represents a configuration history entry
/// </summary>
public class ConfigurationHistoryDto
{
    /// <summary>
    /// Unique identifier for the history entry
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the configuration change
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User who made the change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Backup file path
    /// </summary>
    public string BackupFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the current active configuration
    /// </summary>
    public bool IsActive { get; set; }
}
