using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Ce.Gateway.Api.Controllers.Pages
{
    [Authorize]
    [Route("nodestatus")]
    public class NodeStatusController : Controller
    {
        private readonly IConfiguration _configuration;

        public NodeStatusController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var evaluationTimeInSeconds = _configuration.GetValue<int?>("HealthChecksUI:EvaluationTimeInSeconds") ?? 60;
            ViewBag.EvaluationTimeInSeconds = evaluationTimeInSeconds;
            return View();
        }
    }
}
