using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    /// <summary>
    /// MVC Controller for route configuration management pages
    /// </summary>
    [Authorize(Roles = "Administrator")]
    [Route("routes")]
    public class RouteConfigController : Controller
    {
        /// <summary>
        /// Route configuration management index page
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Configuration history page
        /// </summary>
        [HttpGet("history")]
        public IActionResult History()
        {
            return View();
        }
    }
}
