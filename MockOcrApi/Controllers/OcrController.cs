using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MockOcrApi.Controllers
{
    [ApiController]
    [Route("ocr")]
    public class OcrController : ControllerBase
    {
        private readonly ILogger<OcrController> _logger;
        private static readonly Random _random = new Random();

        public OcrController(ILogger<OcrController> logger)
        {
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessOcr([FromBody] OcrRequestDto request)
        {
            return await HandleOcrRequest($"process (FileId: {request.FileId}, FilePath: {request.FilePath})");
        }

        [HttpGet("cccd")]
        public async Task<IActionResult> GetCccd()
        {
            return await HandleOcrRequest("cccd");
        }

        [HttpGet("hopdong")]
        public async Task<IActionResult> GetHopDong()
        {
            return await HandleOcrRequest("hopdong");
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            return await HandleOcrRequest("summary");
        }

        private async Task<IActionResult> HandleOcrRequest(string route)
        {
            var delay = _random.Next(100, 2000);
            await Task.Delay(delay);

            var randomCase = _random.Next(1, 100);
           
            
            if (randomCase == 1 )
            {
                var errorMessage = "Mockup ratelimit node return 423 code ";
                _logger.LogInformation($"Route: /ocr/{route}, Delay: {delay}ms, Result: Error - {errorMessage}");
                return StatusCode(423, errorMessage);
            }

            var result = new { status = "Success" };
            _logger.LogInformation($"Route: /ocr/{route}, Delay: {delay}ms, Result: Success");
            return Ok(result);
        }
    }
}

public class OcrRequestDto
{
    public string FileId { get; set; }
    public string FilePath { get; set; }
}
