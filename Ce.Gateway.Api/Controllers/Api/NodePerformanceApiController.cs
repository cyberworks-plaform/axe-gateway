using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    [ApiController]
    [Route("api/nodeperformance")]
    public class NodePerformanceApiController : ControllerBase
    {
        private readonly INodePerformanceService _nodePerformanceService;

        public NodePerformanceApiController(INodePerformanceService nodePerformanceService)
        {
            _nodePerformanceService = nodePerformanceService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetNodePerformanceSummary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var summary = await _nodePerformanceService.GetNodePerformanceSummaryAsync(from, to);
            return Ok(summary);
        }

        [HttpGet("requestspernode")]
        public async Task<IActionResult> GetRequestsPerNode(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var data = await _nodePerformanceService.GetRequestsPerNodeAsync(from, to);
            return Ok(data);
        }

        [HttpGet("averagelatencypernode")]
        public async Task<IActionResult> GetAverageLatencyPerNode(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var data = await _nodePerformanceService.GetAverageLatencyPerNodeAsync(from, to);
            return Ok(data);
        }

        [HttpGet("errorratepernode")]
        public async Task<IActionResult> GetErrorRatePerNode(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var data = await _nodePerformanceService.GetErrorRatePerNodeAsync(from, to);
            return Ok(data);
        }

        [HttpGet("topslownessrequests")]
        public async Task<IActionResult> GetTopNSlowestRequests(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int n = 10)
        {
            var data = await _nodePerformanceService.GetTopNSlowestRequestsAsync(from, to, n);
            return Ok(data);
        }
    }
}
