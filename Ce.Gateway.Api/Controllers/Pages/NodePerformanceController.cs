using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Route("nodeperformance")]
    public class NodePerformanceController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
