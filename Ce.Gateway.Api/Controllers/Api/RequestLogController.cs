using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    [ApiController]
    [Route("api/requestlog")]
    public class RequestLogController : ControllerBase
    {
        private readonly IRequestLogService _requestLogService;

        public RequestLogController(IRequestLogService requestLogService)
        {
            _requestLogService = requestLogService;
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
            if (pageSize > 100) pageSize = 100;

            var filter = new LogFilter
            {
                DownstreamHost = downstreamHost,
                UpstreamClientIp = upstreamClientIp,
                DownstreamStatusCode = downstreamStatusCode,
                From = from,
                To = to
            };

            var result = await _requestLogService.GetLogsAsync(filter, page, pageSize);
            return Ok(result);
        }
    }
}
