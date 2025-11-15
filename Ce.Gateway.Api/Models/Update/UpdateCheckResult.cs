using System;

namespace Ce.Gateway.Api.Models.Update
{
    /// <summary>
    /// Result of checking for available updates
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// Current installed version
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// Latest available version
        /// </summary>
        public string? LatestVersion { get; set; }

        /// <summary>
        /// Whether an update is available
        /// </summary>
        public bool UpdateAvailable { get; set; }

        /// <summary>
        /// Download URL for the update
        /// </summary>
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Release notes for the new version
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// Published date of the new version
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// File name of the update package
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Git commit hash
        /// </summary>
        public string? GitHash { get; set; }

        /// <summary>
        /// Error message if check failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// When the check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    }
}
