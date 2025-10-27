using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers
{
    [ApiController]
    public class NodeHealthController : Controller
    {
        private readonly IDownstreamHealthStore _downstreamHealthStore;
        private readonly IConfiguration _configuration;

        public NodeHealthController(IDownstreamHealthStore downstreamHealthStore, IConfiguration configuration)
        {
            _downstreamHealthStore = downstreamHealthStore;
            _configuration = configuration;
        }

        [HttpGet("api/nodehealth")]
        public async Task<IActionResult> GetNodeHealth()
        {
            var healthStatus = await _downstreamHealthStore.GetAllHealthAsync();
            return Ok(healthStatus);
        }

        [HttpGet("nodehealth/ui")]
        public IActionResult NodeHealthUI()
        {
            var evaluationTimeInSeconds = _configuration.GetValue<int?>("HealthChecksUI:EvaluationTimeInSeconds") ?? 60;
            ViewBag.EvaluationTimeInSeconds = evaluationTimeInSeconds;
            return View("~/Views/NodeHealth/Index.cshtml");
        }
    }
}
