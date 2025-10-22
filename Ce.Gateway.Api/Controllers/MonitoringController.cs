using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers
{
    [Route("api/monitor")]
    [ApiController]
    public class MonitoringController : ControllerBase
    {
        private readonly IMonitoringService _monitoringService;

        public MonitoringController(IMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string route = null,
            [FromQuery] string node = null,
            [FromQuery] int? statusCode = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;
            if (pageSize > 100) pageSize = 100; // Limit page size

            var filter = new LogFilter
            {
                Route = route,
                Node = node,
                StatusCode = statusCode,
                From = from,
                To = to
            };

            var result = await _monitoringService.GetLogsAsync(filter, page, pageSize);
            return Ok(result);
        }
    }
}
