using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Models.Update;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// API controller for system update management
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class UpdateController : ControllerBase
    {
        private readonly IUpdateService _updateService;

        public UpdateController(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        /// <summary>
        /// Check for available updates
        /// </summary>
        [HttpGet("check")]
        public async Task<ActionResult<ApiResponse<UpdateCheckResult>>> CheckForUpdates(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _updateService.CheckForUpdatesAsync(cancellationToken);
                return Ok(ApiResponse<UpdateCheckResult>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UpdateCheckResult>.ErrorResult($"Error checking for updates: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get current version
        /// </summary>
        [HttpGet("version")]
        public ActionResult<ApiResponse<string>> GetVersion()
        {
            var version = _updateService.GetCurrentVersion();
            return Ok(ApiResponse<string>.SuccessResult(version));
        }

        /// <summary>
        /// Get update history
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<object>>> GetHistory(CancellationToken cancellationToken)
        {
            try
            {
                var updates = await _updateService.GetUpdateHistoryAsync(cancellationToken);
                return Ok(ApiResponse<object>.SuccessResult(new { updates }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult($"Error retrieving update history: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get specific update details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UpdateDto>>> GetUpdate(int id, CancellationToken cancellationToken)
        {
            try
            {
                var update = await _updateService.GetUpdateByIdAsync(id, cancellationToken);
                if (update == null)
                {
                    return NotFound(ApiResponse<UpdateDto>.ErrorResult("Update not found"));
                }
                return Ok(ApiResponse<UpdateDto>.SuccessResult(update));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UpdateDto>.ErrorResult($"Error retrieving update: {ex.Message}"));
            }
        }

        /// <summary>
        /// Download a specific update
        /// </summary>
        [HttpPost("{id}/download")]
        public async Task<ActionResult<ApiResponse<object>>> DownloadUpdate(int id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _updateService.DownloadUpdateAsync(id, cancellationToken);
                if (success)
                {
                    return Ok(ApiResponse<object>.SuccessResult(new { message = "Update downloaded successfully" }));
                }
                return BadRequest(ApiResponse<object>.ErrorResult("Failed to download update"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult($"Error downloading update: {ex.Message}"));
            }
        }

        /// <summary>
        /// Upload an update package manually
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(500_000_000)] // 500 MB limit
        public async Task<ActionResult<ApiResponse<UpdateDto>>> UploadUpdate(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<UpdateDto>.ErrorResult("No file provided"));
            }

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<UpdateDto>.ErrorResult("Only ZIP files are allowed"));
            }

            try
            {
                var userName = User.Identity?.Name ?? "Unknown";
                await using var stream = file.OpenReadStream();
                var update = await _updateService.UploadUpdateAsync(file.FileName, stream, userName, cancellationToken);
                
                return Ok(ApiResponse<UpdateDto>.SuccessResult(update));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<UpdateDto>.ErrorResult($"Error uploading update: {ex.Message}"));
            }
        }

        /// <summary>
        /// Apply an update
        /// </summary>
        [HttpPost("{id}/apply")]
        public async Task<ActionResult<ApiResponse<ApplyUpdateResponse>>> ApplyUpdate(
            int id,
            [FromBody] ApplyUpdateRequest? request,
            CancellationToken cancellationToken)
        {
            try
            {
                var createBackup = request?.CreateBackup ?? true;
                var userName = User.Identity?.Name ?? "Unknown";
                
                var result = await _updateService.ApplyUpdateAsync(id, createBackup, userName, cancellationToken);
                
                if (result.Success)
                {
                    return Ok(ApiResponse<ApplyUpdateResponse>.SuccessResult(result));
                }
                return BadRequest(ApiResponse<ApplyUpdateResponse>.ErrorResult(result.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<ApplyUpdateResponse>.ErrorResult($"Error applying update: {ex.Message}"));
            }
        }

        /// <summary>
        /// Delete an update
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUpdate(int id, CancellationToken cancellationToken)
        {
            try
            {
                var success = await _updateService.DeleteUpdateAsync(id, cancellationToken);
                if (success)
                {
                    return Ok(ApiResponse<object>.SuccessResult(new { message = "Update deleted successfully" }));
                }
                return BadRequest(ApiResponse<object>.ErrorResult("Failed to delete update or update is current version"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult($"Error deleting update: {ex.Message}"));
            }
        }

        /// <summary>
        /// Rollback to a previous backup
        /// </summary>
        [HttpPost("rollback")]
        public async Task<ActionResult<ApiResponse<object>>> Rollback([FromBody] RollbackRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.BackupPath))
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Backup path is required"));
            }

            try
            {
                var success = await _updateService.RollbackAsync(request.BackupPath, cancellationToken);
                if (success)
                {
                    return Ok(ApiResponse<object>.SuccessResult(new { message = "Rollback initiated. Application will restart shortly." }));
                }
                return BadRequest(ApiResponse<object>.ErrorResult("Rollback failed. Backup file may not exist."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult($"Error during rollback: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Request model for rollback operation
    /// </summary>
    public class RollbackRequest
    {
        public string BackupPath { get; set; } = string.Empty;
    }
}
