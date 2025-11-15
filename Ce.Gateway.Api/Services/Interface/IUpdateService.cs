using Ce.Gateway.Api.Models.Update;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    /// <summary>
    /// Service for managing system updates
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>
        /// Check for available updates from remote source
        /// </summary>
        Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Download an update package
        /// </summary>
        /// <param name="updateId">ID of the update record</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<bool> DownloadUpdateAsync(int updateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload an update package manually
        /// </summary>
        /// <param name="fileName">Name of the file</param>
        /// <param name="fileStream">Stream containing the file data</param>
        /// <param name="initiatedBy">User who initiated the upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<UpdateDto> UploadUpdateAsync(string fileName, System.IO.Stream fileStream, string initiatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Apply a downloaded/uploaded update
        /// </summary>
        /// <param name="updateId">ID of the update to apply</param>
        /// <param name="createBackup">Whether to create a backup before applying</param>
        /// <param name="initiatedBy">User who initiated the update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<ApplyUpdateResponse> ApplyUpdateAsync(int updateId, bool createBackup, string initiatedBy, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all update records
        /// </summary>
        Task<List<UpdateDto>> GetUpdateHistoryAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific update by ID
        /// </summary>
        Task<UpdateDto?> GetUpdateByIdAsync(int updateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete an update record and its associated files
        /// </summary>
        Task<bool> DeleteUpdateAsync(int updateId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the current installed version
        /// </summary>
        string GetCurrentVersion();

        /// <summary>
        /// Rollback to a previous backup
        /// </summary>
        /// <param name="backupPath">Path to the backup to restore</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<bool> RollbackAsync(string backupPath, CancellationToken cancellationToken = default);
    }
}
