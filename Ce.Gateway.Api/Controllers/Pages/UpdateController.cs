using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ce.Gateway.Api.Controllers.Pages
{
    /// <summary>
    /// Page controller for system update management UI
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("update")]
    public class UpdateController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
