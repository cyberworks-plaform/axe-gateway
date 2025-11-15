using System;
using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Entities;

/// <summary>
/// Entity for storing configuration history
/// </summary>
public class ConfigurationHistory
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp of the configuration change
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who made the change
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Description of the change
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Backup file name (relative to backup directory)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string BackupFileName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the current active configuration
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Version information (e.g., "2.4.3")
    /// </summary>
    [MaxLength(50)]
    public string? Version { get; set; }

    /// <summary>
    /// Change type: Manual, Upload, Rollback
    /// </summary>
    [MaxLength(50)]
    public string ChangeType { get; set; } = "Manual";
}
