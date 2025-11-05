using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    [ApiController]
    [Route("api/requestreport")]
    public class RequestReportController : ControllerBase
    {
        private readonly ILogRepository _logRepository;

        public RequestReportController(ILogRepository logRepository)
        {
            _logRepository = logRepository;
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetReportData([FromQuery] string period = "1d")
        {
            var to = DateTime.UtcNow;
            DateTime from;
            string groupBy;

            switch (period)
            {
                case "1d":
                    from = to.AddDays(-1);
                    groupBy = "hour";
                    break;
                case "7d":
                    from = to.AddDays(-7);
                    groupBy = "day";
                    break;
                case "1m":
                    from = to.AddMonths(-1);
                    groupBy = "day";
                    break;
                case "3m":
                    from = to.AddMonths(-3);
                    groupBy = "month";
                    break;
                case "9m":
                    from = to.AddMonths(-9);
                    groupBy = "month";
                    break;
                case "12m":
                    from = to.AddMonths(-12);
                    groupBy = "month";
                    break;
                default:
                    from = to.AddDays(-1);
                    groupBy = "hour";
                    break;
            }

            var report = await _logRepository.GetRequestReportAsync(from, to, groupBy);
            return Ok(report);
        }
    }
}
