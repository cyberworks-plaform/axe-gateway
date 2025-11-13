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

            // Determine from/to based on period
            switch (period)
            {
                case "15m":
                    from = to.AddMinutes(-15);
                    break;
                case "30m":
                    from = to.AddMinutes(-30);
                    break;
                case "1h":
                    from = to.AddHours(-1);
                    break;
                case "6h":
                    from = to.AddHours(-6);
                    break;
                case "12h":
                    from = to.AddHours(-12);
                    break;
                case "1d":
                    from = to.AddDays(-1);
                    break;
                case "7d":
                    from = to.AddDays(-7);
                    break;
                case "1m":
                    from = to.AddMonths(-1);
                    break;
                case "3m":
                    from = to.AddMonths(-3);
                    break;
                case "9m":
                    from = to.AddMonths(-9);
                    break;
                case "12m":
                    from = to.AddMonths(-12);
                    break;
                default:
                    from = to.AddDays(-1);
                    break;
            }

            // Determine granularity based on time range
            // Logic: ≤1h → minute, ≤1d → hour, ≤30d → day, >30d → month
            var duration = to - from;
            if (duration.TotalHours <= 1)
            {
                granularity = Granularity.Minute;
            }
            else if (duration.TotalDays <= 1)
            {
                granularity = Granularity.Hour;
            }
            else if (duration.TotalDays <= 30)
            {
                granularity = Granularity.Day;
            }
            else
            {
                granularity = Granularity.Month;
            }

            var report = await _reportService.GetReportAsync(from, to, granularity);
            return Ok(report);
        }
    }
}
