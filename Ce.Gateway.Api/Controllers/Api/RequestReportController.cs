using Ce.Gateway.Api.Models;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    [ApiController]
    [Route("api/requestreport")]
    public class RequestReportController : ControllerBase
    {
        private readonly IRequestReportService _reportService;

        public RequestReportController(IRequestReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetReportData([FromQuery] string period = "1d")
        {
            var to = DateTime.UtcNow;
            DateTime from;
            Granularity granularity;

            switch (period)
            {
                case "1d":
                    from = to.AddDays(-1);
                    granularity = Granularity.Hour;
                    break;
                case "7d":
                    from = to.AddDays(-7);
                    granularity = Granularity.Day;
                    break;
                case "1m":
                    from = to.AddMonths(-1);
                    granularity = Granularity.Day;
                    break;
                case "3m":
                    from = to.AddMonths(-3);
                    granularity = Granularity.Month;
                    break;
                case "9m":
                    from = to.AddMonths(-9);
                    granularity = Granularity.Month;
                    break;
                case "12m":
                    from = to.AddMonths(-12);
                    granularity = Granularity.Month;
                    break;
                default:
                    from = to.AddDays(-1);
                    granularity = Granularity.Hour;
                    break;
            }

            var report = await _reportService.GetReportAsync(from, to, granularity);
            return Ok(report);
        }
    }
}
