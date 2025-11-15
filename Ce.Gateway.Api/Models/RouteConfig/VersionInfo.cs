namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Version information for configuration
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Semantic version (e.g., "2.4.3")
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Git commit hash (short form)
    /// </summary>
    public string? GitHash { get; set; }

    /// <summary>
    /// Timestamp of build
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// Full version string
    /// </summary>
    public string FullVersion => $"v{Version}" + (string.IsNullOrEmpty(GitHash) ? "" : $"-{GitHash}");

    /// <summary>
    /// Parse version from semantic version string
    /// </summary>
    public bool TryParseSemVer(out int major, out int minor, out int patch)
    {
        major = minor = patch = 0;
        
        if (string.IsNullOrEmpty(Version))
            return false;

        var parts = Version.Split('.');
        if (parts.Length != 3)
            return false;

        return int.TryParse(parts[0], out major) &&
               int.TryParse(parts[1], out minor) &&
               int.TryParse(parts[2], out patch);
    }

    /// <summary>
    /// Compare this version with another version
    /// Returns: -1 if this &lt; other, 0 if equal, 1 if this &gt; other
    /// </summary>
    public int CompareTo(VersionInfo? other)
    {
        if (other == null)
            return 1;

        if (!TryParseSemVer(out int thisMajor, out int thisMinor, out int thisPatch))
            return 0;

        if (!other.TryParseSemVer(out int otherMajor, out int otherMinor, out int otherPatch))
            return 0;

        if (thisMajor != otherMajor)
            return thisMajor.CompareTo(otherMajor);
        
        if (thisMinor != otherMinor)
            return thisMinor.CompareTo(otherMinor);
        
        return thisPatch.CompareTo(otherPatch);
    }

    /// <summary>
    /// Check if this version is a downgrade compared to other
    /// </summary>
    public bool IsDowngradeFrom(VersionInfo? other)
    {
        return CompareTo(other) < 0;
    }

    /// <summary>
    /// Check if this version is an upgrade compared to other
    /// </summary>
    public bool IsUpgradeFrom(VersionInfo? other)
    {
        return CompareTo(other) > 0;
    }
}
