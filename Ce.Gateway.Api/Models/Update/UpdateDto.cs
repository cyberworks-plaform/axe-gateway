using System;

namespace Ce.Gateway.Api.Models.Update
{
    /// <summary>
    /// DTO for system update information
    /// </summary>
    public class UpdateDto
    {
        public int Id { get; set; }
        public string Version { get; set; } = string.Empty;
        public string? GitHash { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DownloadedAt { get; set; }
        public DateTime? InstallStartedAt { get; set; }
        public DateTime? InstallCompletedAt { get; set; }
        public string? InitiatedBy { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsCurrentVersion { get; set; }
    }

    /// <summary>
    /// Request to apply an update
    /// </summary>
    public class ApplyUpdateRequest
    {
        /// <summary>
        /// ID of the update to apply (from SystemUpdate table)
        /// </summary>
        public int UpdateId { get; set; }

        /// <summary>
        /// Whether to create a backup before applying
        /// </summary>
        public bool CreateBackup { get; set; } = true;
    }

    /// <summary>
    /// Response after applying an update
    /// </summary>
    public class ApplyUpdateResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public DateTime? RestartScheduledAt { get; set; }
    }

    /// <summary>
    /// Configuration for update settings
    /// </summary>
    public class UpdateSettings
    {
        /// <summary>
        /// GitHub repository owner
        /// </summary>
        public string? GitHubOwner { get; set; }

        /// <summary>
        /// GitHub repository name
        /// </summary>
        public string? GitHubRepo { get; set; }

        /// <summary>
        /// Alternative update URL (if not using GitHub)
        /// </summary>
        public string? UpdateUrl { get; set; }

        /// <summary>
        /// Whether to automatically check for updates
        /// </summary>
        public bool AutoCheckEnabled { get; set; } = false;

        /// <summary>
        /// Interval in hours for automatic update checks
        /// </summary>
        public int AutoCheckIntervalHours { get; set; } = 24;

        /// <summary>
        /// Whether to automatically download updates
        /// </summary>
        public bool AutoDownloadEnabled { get; set; } = false;

        /// <summary>
        /// Whether to automatically install updates
        /// </summary>
        public bool AutoInstallEnabled { get; set; } = false;

        /// <summary>
        /// Directory to store update packages
        /// </summary>
        public string UpdatesDirectory { get; set; } = "updates";

        /// <summary>
        /// Directory to store backups
        /// </summary>
        public string BackupsDirectory { get; set; } = "backups";

        /// <summary>
        /// Maximum number of backups to keep
        /// </summary>
        public int MaxBackupsToKeep { get; set; } = 5;
    }
}
