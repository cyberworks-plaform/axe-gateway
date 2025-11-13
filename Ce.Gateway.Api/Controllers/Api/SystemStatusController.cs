using Ce.Gateway.Api.Models.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// System status API for monitoring gateway health and metrics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemStatusController : ControllerBase
    {
        private static readonly DateTime _startTime = DateTime.UtcNow;
        private static long _totalRequests = 0;
        private static int _currentActiveRequests = 0;

        /// <summary>
        /// Get system status including uptime and request metrics
        /// </summary>
        [HttpGet]
        public ActionResult<ApiResponse<SystemStatusDto>> GetStatus()
        {
            var uptime = DateTime.UtcNow - _startTime;
            
            var status = new SystemStatusDto
            {
                Status = "Running",
                StartTime = _startTime,
                Uptime = FormatUptime(uptime),
                UptimeSeconds = (long)uptime.TotalSeconds,
                TotalRequests = _totalRequests,
                ActiveRequests = _currentActiveRequests,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<SystemStatusDto>.SuccessResult(status));
        }

        /// <summary>
        /// Increment request counter (called by middleware)
        /// </summary>
        [NonAction]
        public static void IncrementRequestCount()
        {
            System.Threading.Interlocked.Increment(ref _totalRequests);
        }

        /// <summary>
        /// Increment active request counter
        /// </summary>
        [NonAction]
        public static void IncrementActiveRequests()
        {
            System.Threading.Interlocked.Increment(ref _currentActiveRequests);
        }

        /// <summary>
        /// Decrement active request counter
        /// </summary>
        [NonAction]
        public static void DecrementActiveRequests()
        {
            System.Threading.Interlocked.Decrement(ref _currentActiveRequests);
        }

        private string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalDays >= 1)
                return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
            else if (uptime.TotalHours >= 1)
                return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
            else if (uptime.TotalMinutes >= 1)
                return $"{(int)uptime.TotalMinutes}m {uptime.Seconds}s";
            else
                return $"{(int)uptime.TotalSeconds}s";
        }
    }

    public class SystemStatusDto
    {
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public string Uptime { get; set; }
        public long UptimeSeconds { get; set; }
        public long TotalRequests { get; set; }
        public int ActiveRequests { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
