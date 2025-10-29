using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
