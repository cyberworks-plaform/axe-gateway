using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers
{
    [Route("api/monitor")]
    [ApiController]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;
        private readonly IDownstreamHealthStore _downstreamHealthStore;

        public MonitoringController(IMonitoringService monitoringService, IDownstreamHealthStore downstreamHealthStore)
        {
            _monitoringService = monitoringService;
            _downstreamHealthStore = downstreamHealthStore;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string downstreamHost = null,
            [FromQuery] string upstreamClientIp = null,
            [FromQuery] int? downstreamStatusCode = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100; // Limit page size

            var filter = new LogFilter
            {
                DownstreamHost = downstreamHost,
                UpstreamClientIp = upstreamClientIp,
                DownstreamStatusCode = downstreamStatusCode,
                From = from,
                To = to
            };

            var result = await _monitoringService.GetLogsAsync(filter, page, pageSize);
            return Ok(result);
        }

        [HttpGet("nodestatus/summary")]
        public async Task<IActionResult> GetNodeStatusSummary()
        {
            var healthStatus = await _downstreamHealthStore.GetAllHealthAsync();
            var totalNodes = healthStatus.ToList().Count();
            var nodesUp = healthStatus.Count(s => s.Status == "Healthy");
            var nodesDown = totalNodes - nodesUp;

            var summary = new NodeStatusSummaryDto
            {
                TotalNodes = totalNodes,
                NodesUp = nodesUp,
                NodesDown = nodesDown
            };

            return Ok(summary);
        }

        [HttpGet("downstreamhealth")]
        public async Task<IActionResult> GetDownstreamHealth()
        {
            var healthStatus = await _downstreamHealthStore.GetAllHealthAsync();
            return Ok(healthStatus);
        }
    }
}
