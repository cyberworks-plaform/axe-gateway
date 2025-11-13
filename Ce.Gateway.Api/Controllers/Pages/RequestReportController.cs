using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Authorize]
    [Route("requestreport")]
    public class RequestReportController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
