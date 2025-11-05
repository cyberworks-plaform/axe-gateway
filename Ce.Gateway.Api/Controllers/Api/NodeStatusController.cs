using Ce.Gateway.Api.Repositories.Interface;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Controllers.Api
{
    [ApiController]
    [Route("api/nodestatus")]
    public class NodeStatusController : ControllerBase
    {
        private readonly IDownstreamHealthStore _downstreamHealthStore;

        public NodeStatusController(IDownstreamHealthStore downstreamHealthStore)
        {
            _downstreamHealthStore = downstreamHealthStore;
        }

        [HttpGet]
        public async Task<IActionResult> GetNodeStatus()
        {
            var healthStatus = await _downstreamHealthStore.GetAllHealthAsync();
            return Ok(healthStatus);
        }
    }
}
