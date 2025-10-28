using Ce.Gateway.Api.Models.Dashboard;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
            [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            var overview = await _dashboardService.GetDashboardOverviewAsync(startTime.ToUniversalTime(), endTime.ToUniversalTime());
            return Ok(overview);
        }

        [HttpGet("routesummary")]
        public async Task<ActionResult<List<RouteSummaryDto>>> GetRouteSummary(
            [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            var summary = await _dashboardService.GetRouteSummaryAsync(startTime.ToUniversalTime(), endTime.ToUniversalTime());
            return Ok(summary);
        }

        [HttpGet("nodesummary")]
        public async Task<ActionResult<List<NodeSummaryDto>>> GetNodeSummary(
            [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            var summary = await _dashboardService.GetNodeSummaryAsync(startTime.ToUniversalTime(), endTime.ToUniversalTime());
            return Ok(summary);
        }

        [HttpGet("recenterrors")]
        public async Task<ActionResult<List<ErrorLogDto>>> GetRecentErrors(
            [FromQuery] DateTime startTime, [FromQuery] DateTime endTime, [FromQuery] int limit = 100)
        {
            var errors = await _dashboardService.GetRecentErrorsAsync(startTime.ToUniversalTime(), endTime.ToUniversalTime(), limit);
            return Ok(errors);
        }
    }
}
