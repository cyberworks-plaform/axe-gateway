using System;
using System.ComponentModel.DataAnnotations;

namespace Ce.Gateway.Api.Entities
{
    /// <summary>
    /// Entity representing a system update record
    /// </summary>
    public class SystemUpdate
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Version number (e.g., "2.4.3")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Git commit hash (short or full)
        /// </summary>
        [MaxLength(50)]
        public string? GitHash { get; set; }

        /// <summary>
        /// Update status: Pending, Downloading, Downloaded, Installing, Installed, Failed, RolledBack
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Original file name of the update package
        /// </summary>
        [MaxLength(255)]
        public string? FileName { get; set; }

        /// <summary>
        /// Local file path where the update package is stored
        /// </summary>
        [MaxLength(500)]
        public string? FilePath { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Checksum (MD5 or SHA256) of the update package
        /// </summary>
        [MaxLength(100)]
        public string? Checksum { get; set; }

        /// <summary>
        /// Download URL (if downloaded from remote source)
        /// </summary>
        [MaxLength(500)]
        public string? DownloadUrl { get; set; }

        /// <summary>
        /// Release notes or description
        /// </summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// When the update record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the update was downloaded (if applicable)
        /// </summary>
        public DateTime? DownloadedAt { get; set; }

        /// <summary>
        /// When the update installation started
        /// </summary>
        public DateTime? InstallStartedAt { get; set; }

        /// <summary>
        /// When the update installation completed
        /// </summary>
        public DateTime? InstallCompletedAt { get; set; }

        /// <summary>
        /// User who initiated the update
        /// </summary>
        [MaxLength(100)]
        public string? InitiatedBy { get; set; }

        /// <summary>
        /// Error message if update failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Path to backup created before update
        /// </summary>
        [MaxLength(500)]
        public string? BackupPath { get; set; }

        /// <summary>
        /// Whether this update is currently active/installed
        /// </summary>
        public bool IsCurrentVersion { get; set; }
    }

    /// <summary>
    /// Update status constants
    /// </summary>
    public static class UpdateStatus
    {
        public const string Pending = "Pending";
        public const string Downloading = "Downloading";
        public const string Downloaded = "Downloaded";
        public const string Installing = "Installing";
        public const string Installed = "Installed";
        public const string Failed = "Failed";
        public const string RolledBack = "RolledBack";
    }
}
