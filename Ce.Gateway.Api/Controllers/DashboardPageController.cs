using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers
{
    public class DashboardPageController : Controller
    {
        [HttpGet("/dashboard")]
        public IActionResult Index()
        {
            return File("~/dashboard/dashboard.html", "text/html");
        }
    }
}
