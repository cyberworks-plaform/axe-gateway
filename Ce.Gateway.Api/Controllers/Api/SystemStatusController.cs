using Ce.Gateway.Api.Middleware;
using Ce.Gateway.Api.Models.Common;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    /// <summary>
    /// System status API for monitoring gateway health and metrics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SystemStatusController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public SystemStatusController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get system status including uptime, request metrics, and node health
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<SystemStatusDto>>> GetStatus()
        {
            var metrics = RequestMetricsMiddleware.GetMetrics();
            var uptime = TimeSpan.FromSeconds(metrics.UptimeSeconds);
            
            // Get node health from dashboard service
            var (totalNodes, nodesDown) = await _dashboardService.GetNodeHealthStatsAsync();
            
            var status = new SystemStatusDto
            {
                Status = "Running",
                StartTime = metrics.StartTime,
                Uptime = FormatUptime(uptime),
                UptimeSeconds = metrics.UptimeSeconds,
                TotalRequests = metrics.TotalRequests,
                ActiveRequests = metrics.ActiveRequests,
                TotalNodes = totalNodes,
                NodesDown = nodesDown,
                Timestamp = DateTime.UtcNow
            };

            return Ok(ApiResponse<SystemStatusDto>.SuccessResult(status));
        }

        private static string FormatUptime(TimeSpan uptime)
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
        public int TotalNodes { get; set; }
        public int NodesDown { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
