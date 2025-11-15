# System Update Feature

## Overview

The System Update feature allows administrators to manage application updates through both automatic online checking and manual offline uploads. This feature is designed specifically for Windows Server with IIS deployments.

## Features

### 1. Update Checking
- **Automatic**: Check for updates from GitHub Releases API
- **Manual**: Upload update packages for offline environments
- **Version Comparison**: Automatically compares current version with available updates

### 2. Update Management
- **Download**: Automatically download updates from GitHub (when internet available)
- **Upload**: Manually upload ZIP packages (for offline servers)
- **Validation**: Validate package integrity using SHA256 checksums
- **History**: Track all update activities with audit trail

### 3. Safe Update Application
- **Backup**: Automatic backup before applying updates
- **Rollback**: Restore from backup if update fails
- **Graceful Restart**: Application restarts automatically after update
- **Cleanup**: Automatic cleanup of old backups (configurable retention)

## Configuration

Add the following section to `appsettings.json`:

```json
{
  "Update": {
    "GitHubOwner": "cyberworks-plaform",
    "GitHubRepo": "axe-gateway",
    "UpdateUrl": "",
    "AutoCheckEnabled": false,
    "AutoCheckIntervalHours": 24,
    "AutoDownloadEnabled": false,
    "AutoInstallEnabled": false,
    "UpdatesDirectory": "updates",
    "BackupsDirectory": "backups",
    "MaxBackupsToKeep": 5
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `GitHubOwner` | string | - | GitHub repository owner/organization |
| `GitHubRepo` | string | - | GitHub repository name |
| `UpdateUrl` | string | - | Alternative custom update URL (if not using GitHub) |
| `AutoCheckEnabled` | bool | false | Enable automatic update checking |
| `AutoCheckIntervalHours` | int | 24 | Hours between automatic checks |
| `AutoDownloadEnabled` | bool | false | Automatically download available updates |
| `AutoInstallEnabled` | bool | false | Automatically install downloaded updates (not recommended) |
| `UpdatesDirectory` | string | updates | Directory to store update packages |
| `BackupsDirectory` | string | backups | Directory to store backups |
| `MaxBackupsToKeep` | int | 5 | Maximum number of backups to retain |

## Usage

### For Administrators (Web UI)

1. **Access Update Management**
   - Navigate to `/update` (requires Admin role)
   - View current version and update history

2. **Check for Updates**
   - Click "Check for Updates" button
   - System queries GitHub Releases API
   - Displays available updates if found

3. **Manual Upload (Offline Servers)**
   - Click "Choose file" under Manual Upload section
   - Select the `.zip` update package
   - Click "Upload Update"
   - Package is validated and stored

4. **Apply Update**
   - Click "Apply" button next to desired update
   - System creates backup automatically
   - Application extracts and restarts
   - Update is applied after restart

5. **View History**
   - All update activities are logged
   - View status, dates, and initiator
   - Delete old update packages (except current version)

### For Developers (API)

#### Check for Updates
```http
GET /api/update/check
Authorization: Bearer {token}
```

Response:
```json
{
  "success": true,
  "data": {
    "currentVersion": "2.4.3",
    "latestVersion": "2.5.0",
    "updateAvailable": true,
    "downloadUrl": "https://github.com/.../releases/download/v2.5.0/package.zip",
    "releaseNotes": "## What's New\n- Feature A\n- Bug fix B",
    "publishedAt": "2024-11-15T00:00:00Z",
    "fileName": "Ce.Gateway.Api-v2.5.0-abc123-update.zip",
    "fileSize": 52428800
  }
}
```

#### Upload Update Package
```http
POST /api/update/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: {binary data}
```

Response:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "version": "2.5.0",
    "status": "Downloaded",
    "fileName": "update-v2.5.0.zip",
    "fileSize": 52428800,
    "createdAt": "2024-11-15T10:00:00Z",
    "initiatedBy": "admin"
  }
}
```

#### Apply Update
```http
POST /api/update/{id}/apply
Authorization: Bearer {token}
Content-Type: application/json

{
  "createBackup": true
}
```

Response:
```json
{
  "success": true,
  "data": {
    "success": true,
    "message": "Update applied successfully. Application will restart shortly.",
    "backupPath": "backups/backup-v2.4.3-20241115100000.zip",
    "restartScheduledAt": "2024-11-15T10:00:05Z"
  }
}
```

#### Get Update History
```http
GET /api/update/history
Authorization: Bearer {token}
```

#### Get Current Version
```http
GET /api/update/version
Authorization: Bearer {token}
```

#### Delete Update
```http
DELETE /api/update/{id}
Authorization: Bearer {token}
```

## Deployment Workflow

### Current Manual Process
1. Developer runs `build.ps1` to create ZIP package
2. Copy ZIP to server
3. Stop IIS app pool
4. Backup current files
5. Extract ZIP to application directory
6. Start app pool

### New Automated Process

#### Online Scenario
1. Admin clicks "Check for Updates"
2. System finds new version on GitHub
3. Admin clicks "Download Update"
4. System downloads and validates package
5. Admin clicks "Apply"
6. System automatically:
   - Creates backup
   - Stops application
   - Extracts update
   - Restarts application

#### Offline Scenario
1. Developer creates update package using `build.ps1`
2. Admin uploads ZIP through web interface
3. System validates and stores package
4. Admin clicks "Apply"
5. System automatically completes update

## Build Script Integration

The existing `Ce.Gateway.Api/build.ps1` already creates properly formatted update packages:

```powershell
# Output format: AppName-vX.Y.Z-gitHash-update-timestamp.zip
# Example: Ce.Gateway.Api-v2.4.3-abc1234-update-202411151030.zip
```

### Package Structure
The ZIP should contain:
```
Ce.Gateway.Api.dll
Ce.Gateway.Api.pdb
appsettings.publish.json  # Renamed to prevent config override
configuration.publish.json
wwwroot/
Views/
... (all published files)
```

## Safety Features

### 1. Backup Strategy
- Automatic backup before each update
- Excludes: logs, data, updates, backups
- Retains last N backups (configurable)
- Timestamp-based backup naming

### 2. Validation
- File format validation (ZIP only)
- Checksum calculation (SHA256)
- Package content validation
- Version extraction from filename

### 3. Rollback
- Manual rollback from web UI
- Restore from any backup
- Same update process in reverse

### 4. Error Handling
- All operations logged
- Failed updates tracked in database
- Error messages captured
- Status updates throughout process

### 5. Security
- Admin-only access
- Audit trail (who, when, what)
- No automatic installation by default
- File path sanitization

## Database Schema

### SystemUpdate Table

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Version | string(50) | Version number (e.g., "2.4.3") |
| GitHash | string(50) | Git commit hash |
| Status | string(20) | Update status |
| FileName | string(255) | Original filename |
| FilePath | string(500) | Local file path |
| FileSize | long | File size in bytes |
| Checksum | string(100) | SHA256 checksum |
| DownloadUrl | string(500) | Source URL (if downloaded) |
| ReleaseNotes | text | Release notes |
| CreatedAt | datetime | Record creation time |
| DownloadedAt | datetime | Download completion time |
| InstallStartedAt | datetime | Installation start time |
| InstallCompletedAt | datetime | Installation completion time |
| InitiatedBy | string(100) | User who initiated |
| ErrorMessage | text | Error details if failed |
| BackupPath | string(500) | Path to backup |
| IsCurrentVersion | bool | Currently installed version |

### Status Values
- `Pending` - Update record created, not downloaded
- `Downloading` - Download in progress
- `Downloaded` - Ready for installation
- `Installing` - Installation in progress
- `Installed` - Successfully installed
- `Failed` - Installation failed
- `RolledBack` - Rolled back after failure

## Troubleshooting

### Update Package Not Recognized
**Problem**: Version not extracted from filename

**Solution**: Use naming format: `AppName-vX.Y.Z-hash-update-timestamp.zip`

### Application Won't Restart
**Problem**: IIS app pool not restarting automatically

**Solution**: 
1. Check IIS app pool settings
2. Verify update batch script permissions
3. Manually restart app pool if needed

### Backup Failed
**Problem**: Insufficient disk space

**Solution**:
1. Free up disk space
2. Reduce `MaxBackupsToKeep` setting
3. Manually delete old backups from `backups/` directory

### Update Package Validation Failed
**Problem**: ZIP file corrupt or invalid

**Solution**:
1. Re-download/re-create package
2. Verify build.ps1 completed successfully
3. Check file integrity before upload

### Rollback Not Working
**Problem**: Backup file missing or corrupt

**Solution**:
1. Use alternative backup if available
2. Manually extract previous version
3. Contact support with error details

## Best Practices

### For Administrators
1. **Always create backups** before applying updates
2. **Test updates** in development environment first
3. **Monitor update logs** for errors
4. **Keep update packages** until verified stable
5. **Schedule updates** during low-traffic periods
6. **Verify application** works after update

### For Developers
1. **Follow semantic versioning** (X.Y.Z)
2. **Include release notes** in GitHub releases
3. **Test build.ps1** before pushing
4. **Document breaking changes** in release notes
5. **Tag releases** properly in Git
6. **Test update process** before production release

### For Operations
1. **Monitor disk space** in updates and backups directories
2. **Review update history** periodically
3. **Clean up failed updates** 
4. **Document custom configurations** that need preservation
5. **Set up alerts** for failed updates
6. **Backup database** before major updates

## Future Enhancements

Potential improvements for future versions:

- [ ] Background worker for automatic update checking
- [ ] Email notifications for available updates
- [ ] Scheduled update windows
- [ ] Multi-server deployment coordination
- [ ] Update preview/dry-run mode
- [ ] Automatic database migration verification
- [ ] Integration with monitoring systems
- [ ] Custom pre/post update scripts
- [ ] Update approval workflow
- [ ] Delta updates (patch files)

## Support

For issues or questions:
- Check logs in `logs/` directory
- Review update history in database
- Contact development team with:
  - Current version
  - Update package name
  - Error messages
  - Update history logs
