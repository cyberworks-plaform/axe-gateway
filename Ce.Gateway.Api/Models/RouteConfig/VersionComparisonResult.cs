using System.Collections.Generic;

namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Result of version comparison for configuration upload
/// </summary>
public class VersionComparisonResult
{
    /// <summary>
    /// Current system version
    /// </summary>
    public VersionInfo CurrentVersion { get; set; } = new();

    /// <summary>
    /// Upload/target version
    /// </summary>
    public VersionInfo UploadVersion { get; set; } = new();

    /// <summary>
    /// Is the upload version a downgrade?
    /// </summary>
    public bool IsDowngrade { get; set; }

    /// <summary>
    /// Is the upload version an upgrade?
    /// </summary>
    public bool IsUpgrade { get; set; }

    /// <summary>
    /// Is the upload version the same?
    /// </summary>
    public bool IsSameVersion { get; set; }

    /// <summary>
    /// Risk warnings to display to user
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Comparison result message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
