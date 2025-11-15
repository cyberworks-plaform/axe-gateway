using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models.Update;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    /// <summary>
    /// Service for managing system updates
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly IDbContextFactory<GatewayDbContext> _dbContextFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UpdateService> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UpdateSettings _settings;
        private static readonly SemaphoreSlim _updateLock = new(1, 1);

        public UpdateService(
            IDbContextFactory<GatewayDbContext> dbContextFactory,
            IConfiguration configuration,
            ILogger<UpdateService> logger,
            IHostApplicationLifetime applicationLifetime,
            IHttpClientFactory httpClientFactory)
        {
            _dbContextFactory = dbContextFactory;
            _configuration = configuration;
            _logger = logger;
            _applicationLifetime = applicationLifetime;
            _httpClientFactory = httpClientFactory;

            // Load settings from configuration
            _settings = new UpdateSettings();
            configuration.GetSection("Update").Bind(_settings);

            // Ensure directories exist
            Directory.CreateDirectory(_settings.UpdatesDirectory);
            Directory.CreateDirectory(_settings.BackupsDirectory);
        }

        public string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString(3) ?? "0.0.0";
        }

        public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
        {
            var currentVersion = GetCurrentVersion();
            var result = new UpdateCheckResult
            {
                CurrentVersion = currentVersion,
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                if (!string.IsNullOrEmpty(_settings.GitHubOwner) && !string.IsNullOrEmpty(_settings.GitHubRepo))
                {
                    // Check GitHub releases
                    var latestRelease = await GetLatestGitHubReleaseAsync(cancellationToken);
                    if (latestRelease != null)
                    {
                        result.LatestVersion = latestRelease.TagName?.TrimStart('v');
                        result.ReleaseNotes = latestRelease.Body;
                        result.PublishedAt = latestRelease.PublishedAt;

                        // Find the appropriate asset (zip file)
                        var asset = latestRelease.Assets?.FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                        if (asset != null)
                        {
                            result.DownloadUrl = asset.BrowserDownloadUrl;
                            result.FileName = asset.Name;
                            result.FileSize = asset.Size;
                        }

                        // Compare versions
                        if (!string.IsNullOrEmpty(result.LatestVersion))
                        {
                            result.UpdateAvailable = IsNewerVersion(currentVersion, result.LatestVersion);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(_settings.UpdateUrl))
                {
                    // Check custom update URL
                    var updateInfo = await CheckCustomUpdateUrlAsync(cancellationToken);
                    if (updateInfo != null)
                    {
                        result.LatestVersion = updateInfo.Version;
                        result.DownloadUrl = updateInfo.DownloadUrl;
                        result.ReleaseNotes = updateInfo.ReleaseNotes;
                        result.FileName = updateInfo.FileName;
                        result.FileSize = updateInfo.FileSize;
                        result.UpdateAvailable = IsNewerVersion(currentVersion, updateInfo.Version);
                    }
                }
                else
                {
                    result.ErrorMessage = "No update source configured. Set Update:GitHubOwner/GitHubRepo or Update:UpdateUrl in configuration.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                result.ErrorMessage = $"Error checking for updates: {ex.Message}";
            }

            return result;
        }

        public async Task<bool> DownloadUpdateAsync(int updateId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var update = await context.SystemUpdates.FindAsync(new object[] { updateId }, cancellationToken);

            if (update == null)
            {
                _logger.LogWarning("Update {UpdateId} not found", updateId);
                return false;
            }

            if (string.IsNullOrEmpty(update.DownloadUrl))
            {
                _logger.LogWarning("Update {UpdateId} has no download URL", updateId);
                return false;
            }

            try
            {
                update.Status = UpdateStatus.Downloading;
                await context.SaveChangesAsync(cancellationToken);

                var fileName = update.FileName ?? $"update-{update.Version}.zip";
                var filePath = Path.Combine(_settings.UpdatesDirectory, fileName);

                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromMinutes(30); // Long timeout for large files

                _logger.LogInformation("Downloading update from {Url}", update.DownloadUrl);
                using var response = await httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var fileStream = File.Create(filePath);
                await response.Content.CopyToAsync(fileStream, cancellationToken);

                var fileInfo = new FileInfo(filePath);
                update.FilePath = filePath;
                update.FileSize = fileInfo.Length;
                update.Checksum = CalculateChecksum(filePath);
                update.DownloadedAt = DateTime.UtcNow;
                update.Status = UpdateStatus.Downloaded;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Update {UpdateId} downloaded successfully to {FilePath}", updateId, filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading update {UpdateId}", updateId);
                update.Status = UpdateStatus.Failed;
                update.ErrorMessage = ex.Message;
                await context.SaveChangesAsync(cancellationToken);
                return false;
            }
        }

        public async Task<UpdateDto> UploadUpdateAsync(string fileName, Stream fileStream, string initiatedBy, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fileStream);
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            try
            {
                // Save file to updates directory
                var safeFileName = Path.GetFileName(fileName);
                var filePath = Path.Combine(_settings.UpdatesDirectory, safeFileName);

                await using (var outputStream = File.Create(filePath))
                {
                    await fileStream.CopyToAsync(outputStream, cancellationToken);
                }

                var fileInfo = new FileInfo(filePath);

                // Extract version from filename or zip content
                var version = ExtractVersionFromFileName(safeFileName);

                // Create update record
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var update = new SystemUpdate
                {
                    Version = version,
                    Status = UpdateStatus.Downloaded,
                    FileName = safeFileName,
                    FilePath = filePath,
                    FileSize = fileInfo.Length,
                    Checksum = CalculateChecksum(filePath),
                    InitiatedBy = initiatedBy,
                    CreatedAt = DateTime.UtcNow,
                    DownloadedAt = DateTime.UtcNow
                };

                context.SystemUpdates.Add(update);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Update {Version} uploaded successfully by {User}", version, initiatedBy);

                return MapToDto(update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading update file {FileName}", fileName);
                throw;
            }
        }

        public async Task<ApplyUpdateResponse> ApplyUpdateAsync(int updateId, bool createBackup, string initiatedBy, CancellationToken cancellationToken = default)
        {
            // Only one update can be applied at a time
            await _updateLock.WaitAsync(cancellationToken);
            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var update = await context.SystemUpdates.FindAsync(new object[] { updateId }, cancellationToken);

                if (update == null)
                {
                    return new ApplyUpdateResponse
                    {
                        Success = false,
                        Message = "Update not found"
                    };
                }

                if (update.Status != UpdateStatus.Downloaded)
                {
                    return new ApplyUpdateResponse
                    {
                        Success = false,
                        Message = $"Update is not ready for installation. Current status: {update.Status}"
                    };
                }

                if (string.IsNullOrEmpty(update.FilePath) || !File.Exists(update.FilePath))
                {
                    return new ApplyUpdateResponse
                    {
                        Success = false,
                        Message = "Update file not found"
                    };
                }

                update.Status = UpdateStatus.Installing;
                update.InstallStartedAt = DateTime.UtcNow;
                update.InitiatedBy = initiatedBy;
                await context.SaveChangesAsync(cancellationToken);

                try
                {
                    // Create backup if requested
                    string? backupPath = null;
                    if (createBackup)
                    {
                        backupPath = await CreateBackupAsync(cancellationToken);
                        update.BackupPath = backupPath;
                        await context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Backup created at {BackupPath}", backupPath);
                    }

                    // Validate update package
                    if (!ValidateUpdatePackage(update.FilePath))
                    {
                        throw new InvalidOperationException("Update package validation failed");
                    }

                    // Schedule application restart with update extraction
                    var restartScheduledAt = DateTime.UtcNow.AddSeconds(5);
                    ScheduleUpdateAndRestart(update.FilePath, restartScheduledAt);

                    update.Status = UpdateStatus.Installed;
                    update.InstallCompletedAt = DateTime.UtcNow;

                    // Mark all other updates as not current
                    await context.SystemUpdates
                        .Where(u => u.IsCurrentVersion)
                        .ForEachAsync(u => u.IsCurrentVersion = false, cancellationToken);

                    update.IsCurrentVersion = true;
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Update {Version} applied successfully. Application will restart shortly.", update.Version);

                    return new ApplyUpdateResponse
                    {
                        Success = true,
                        Message = "Update applied successfully. Application will restart shortly.",
                        BackupPath = backupPath,
                        RestartScheduledAt = restartScheduledAt
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying update {UpdateId}", updateId);
                    update.Status = UpdateStatus.Failed;
                    update.ErrorMessage = ex.Message;
                    await context.SaveChangesAsync(cancellationToken);

                    return new ApplyUpdateResponse
                    {
                        Success = false,
                        Message = $"Error applying update: {ex.Message}"
                    };
                }
            }
            finally
            {
                _updateLock.Release();
            }
        }

        public async Task<List<UpdateDto>> GetUpdateHistoryAsync(CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var updates = await context.SystemUpdates
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            return updates.Select(MapToDto).ToList();
        }

        public async Task<UpdateDto?> GetUpdateByIdAsync(int updateId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var update = await context.SystemUpdates.FindAsync(new object[] { updateId }, cancellationToken);
            return update != null ? MapToDto(update) : null;
        }

        public async Task<bool> DeleteUpdateAsync(int updateId, CancellationToken cancellationToken = default)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var update = await context.SystemUpdates.FindAsync(new object[] { updateId }, cancellationToken);

            if (update == null)
                return false;

            // Don't delete current version
            if (update.IsCurrentVersion)
            {
                _logger.LogWarning("Cannot delete current version {Version}", update.Version);
                return false;
            }

            // Delete associated files
            if (!string.IsNullOrEmpty(update.FilePath) && File.Exists(update.FilePath))
            {
                try
                {
                    File.Delete(update.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete update file {FilePath}", update.FilePath);
                }
            }

            context.SystemUpdates.Remove(update);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Update {Version} deleted", update.Version);
            return true;
        }

        public async Task<bool> RollbackAsync(string backupPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
            {
                _logger.LogError("Backup file not found: {BackupPath}", backupPath);
                return false;
            }

            try
            {
                _logger.LogInformation("Starting rollback from {BackupPath}", backupPath);

                // Schedule rollback and restart
                ScheduleUpdateAndRestart(backupPath, DateTime.UtcNow.AddSeconds(5));

                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var rollbackUpdate = new SystemUpdate
                {
                    Version = "Rollback",
                    Status = UpdateStatus.RolledBack,
                    BackupPath = backupPath,
                    CreatedAt = DateTime.UtcNow,
                    InstallStartedAt = DateTime.UtcNow
                };

                context.SystemUpdates.Add(rollbackUpdate);
                await context.SaveChangesAsync(cancellationToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rollback");
                return false;
            }
        }

        #region Private Helper Methods

        private async Task<GitHubRelease?> GetLatestGitHubReleaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "AxeGateway");

                var url = $"https://api.github.com/repos/{_settings.GitHubOwner}/{_settings.GitHubRepo}/releases/latest";
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GitHub API returned {StatusCode}", response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GitHub release");
                return null;
            }
        }

        private async Task<CustomUpdateInfo?> CheckCustomUpdateUrlAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(_settings.UpdateUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<CustomUpdateInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking custom update URL");
                return null;
            }
        }

        private static bool IsNewerVersion(string current, string latest)
        {
            try
            {
                var currentVer = Version.Parse(current);
                var latestVer = Version.Parse(latest);
                return latestVer > currentVer;
            }
            catch
            {
                return false;
            }
        }

        private string CalculateChecksum(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private bool ValidateUpdatePackage(string zipPath)
        {
            try
            {
                // Basic validation: ensure it's a valid zip and contains expected files
                using var archive = ZipFile.OpenRead(zipPath);
                
                // Check for essential files
                var hasDll = archive.Entries.Any(e => e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
                var hasExe = archive.Entries.Any(e => e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                if (!hasDll && !hasExe)
                {
                    _logger.LogWarning("Update package does not contain expected files");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating update package");
                return false;
            }
        }

        private string ExtractVersionFromFileName(string fileName)
        {
            // Try to extract version from filename pattern: "AppName-vX.Y.Z-hash-update-timestamp.zip"
            try
            {
                var parts = Path.GetFileNameWithoutExtension(fileName).Split('-');
                foreach (var part in parts)
                {
                    if (part.StartsWith("v") && Version.TryParse(part.Substring(1), out _))
                    {
                        return part.Substring(1);
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            // Fallback to timestamp
            return DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        }

        private async Task<string> CreateBackupAsync(CancellationToken cancellationToken)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var currentVersion = GetCurrentVersion();
            var backupFileName = $"backup-v{currentVersion}-{timestamp}.zip";
            var backupPath = Path.Combine(_settings.BackupsDirectory, backupFileName);

            _logger.LogInformation("Creating backup at {BackupPath}", backupPath);

            // Get the application directory
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Create backup zip
            await Task.Run(() =>
            {
                using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
                
                // Backup important files (exclude logs, temp files, updates, backups)
                var excludeDirs = new[] { "logs", "updates", "backups", "data" };
                var excludeExts = new[] { ".log", ".tmp" };

                foreach (var file in Directory.GetFiles(appDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(appDirectory, file);
                    
                    // Skip excluded directories
                    if (excludeDirs.Any(d => relativePath.StartsWith(d + Path.DirectorySeparatorChar)))
                        continue;

                    // Skip excluded extensions
                    if (excludeExts.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    try
                    {
                        archive.CreateEntryFromFile(file, relativePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not backup file {File}", file);
                    }
                }
            }, cancellationToken);

            // Clean up old backups
            await CleanupOldBackupsAsync(cancellationToken);

            return backupPath;
        }

        private async Task CleanupOldBackupsAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                var backupFiles = Directory.GetFiles(_settings.BackupsDirectory, "backup-*.zip")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToList();

                var filesToDelete = backupFiles.Skip(_settings.MaxBackupsToKeep);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        _logger.LogInformation("Deleted old backup: {FileName}", file.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old backup {FileName}", file.Name);
                    }
                }
            }, cancellationToken);
        }

        private void ScheduleUpdateAndRestart(string updatePackagePath, DateTime scheduledTime)
        {
            // On Windows with IIS, we need to extract the update and trigger an app pool recycle
            // Create a batch script that will be executed
            var scriptPath = Path.Combine(Path.GetTempPath(), "axe-gateway-update.bat");
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var extractTempDir = Path.Combine(Path.GetTempPath(), $"axe-gateway-update-{Guid.NewGuid()}");

            var script = $@"@echo off
echo Waiting for application to stop...
timeout /t 10 /nobreak > nul

echo Extracting update package...
mkdir ""{extractTempDir}""
powershell -Command ""Expand-Archive -Path '{updatePackagePath}' -DestinationPath '{extractTempDir}' -Force""

echo Applying update...
xcopy ""{extractTempDir}\*"" ""{appDirectory}"" /E /Y /I

echo Cleaning up...
rmdir /s /q ""{extractTempDir}""

echo Update complete. Application should restart automatically.
del ""%~f0""
";

            File.WriteAllText(scriptPath, script);

            // Start the batch script in a detached process
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start /min cmd /c \"{scriptPath}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process.Start(startInfo);

            // Trigger application shutdown
            _logger.LogInformation("Scheduling application restart...");
            _applicationLifetime.StopApplication();
        }

        private static UpdateDto MapToDto(SystemUpdate entity)
        {
            return new UpdateDto
            {
                Id = entity.Id,
                Version = entity.Version,
                GitHash = entity.GitHash,
                Status = entity.Status,
                FileName = entity.FileName,
                FileSize = entity.FileSize,
                ReleaseNotes = entity.ReleaseNotes,
                CreatedAt = entity.CreatedAt,
                DownloadedAt = entity.DownloadedAt,
                InstallStartedAt = entity.InstallStartedAt,
                InstallCompletedAt = entity.InstallCompletedAt,
                InitiatedBy = entity.InitiatedBy,
                ErrorMessage = entity.ErrorMessage,
                IsCurrentVersion = entity.IsCurrentVersion
            };
        }

        #endregion

        #region Helper Classes

        private class GitHubRelease
        {
            public string? TagName { get; set; }
            public string? Name { get; set; }
            public string? Body { get; set; }
            public DateTime PublishedAt { get; set; }
            public List<GitHubAsset>? Assets { get; set; }
        }

        private class GitHubAsset
        {
            public string Name { get; set; } = string.Empty;
            public string BrowserDownloadUrl { get; set; } = string.Empty;
            public long Size { get; set; }
        }

        private class CustomUpdateInfo
        {
            public string Version { get; set; } = string.Empty;
            public string DownloadUrl { get; set; } = string.Empty;
            public string? ReleaseNotes { get; set; }
            public string? FileName { get; set; }
            public long? FileSize { get; set; }
        }

        #endregion
    }
}
