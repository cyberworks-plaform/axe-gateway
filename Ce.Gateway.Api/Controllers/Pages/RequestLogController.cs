using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Route("requestlog")]
    public class RequestLogController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
