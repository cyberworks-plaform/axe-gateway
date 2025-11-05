using Bogus;

namespace Ce.Gateway.Simulator;

public class RequestLogGenerator
{
    private readonly Faker<RequestLogEntry> _faker;
    private readonly Random _random = new();

    private readonly string[] _httpMethods = { "GET", "POST", "PUT", "DELETE", "PATCH" };
    private readonly string[] _upstreamPaths = { "/api/ocr/process", "/api/ocr/batch", "/api/ocr/status", "/api/documents/upload", "/api/documents/scan" };
    private readonly string[] _downstreamHosts = { "localhost", "ocr-node-1", "ocr-node-2", "ocr-node-3" };
    private readonly int[] _downstreamPorts = { 10501, 10502, 10503 };
    
    // More diverse status codes: 70% success (2xx), 15% client error (4xx), 10% server error (5xx), 5% other
    private readonly int[] _statusCodes = { 
        200, 200, 200, 200, 200, 201, 201, 204,  // 2xx - 8/20
        400, 401, 403, 404,                       // 4xx - 4/20
        500, 502, 503,                            // 5xx - 3/20
        200, 200, 200, 200, 200                   // More 2xx to reach 70%
    };
    
    private readonly string[] _clientIps = { "192.168.1.10", "192.168.1.20", "192.168.1.30", "10.0.0.15", "10.0.0.25" };

    public RequestLogGenerator()
    {
        _faker = new Faker<RequestLogEntry>()
            .RuleFor(r => r.Id, f => Guid.NewGuid())
            .RuleFor(r => r.CreatedAtUtc, f => DateTime.UtcNow.AddSeconds(-_random.Next(0, 10)))
            .RuleFor(r => r.TraceId, f => Guid.NewGuid().ToString())
            
            // Upstream
            .RuleFor(r => r.UpstreamHost, f => "gateway.local")
            .RuleFor(r => r.UpstreamPort, f => 5000)
            .RuleFor(r => r.UpstreamScheme, f => "http")
            .RuleFor(r => r.UpstreamHttpMethod, f => f.PickRandom(_httpMethods))
            .RuleFor(r => r.UpstreamPath, f => f.PickRandom(_upstreamPaths))
            .RuleFor(r => r.UpstreamQueryString, f => f.Random.Bool(0.3f) ? $"?id={f.Random.Number(1000, 9999)}" : string.Empty)
            .RuleFor(r => r.UpstreamRequestSize, f => f.Random.Long(100, 50000))
            .RuleFor(r => r.UpstreamClientIp, f => f.PickRandom(_clientIps))
            
            // Downstream
            .RuleFor(r => r.DownstreamScheme, f => "http")
            .RuleFor(r => r.DownstreamHost, f => f.PickRandom(_downstreamHosts))
            .RuleFor(r => r.DownstreamPort, f => f.PickRandom(_downstreamPorts))
            .RuleFor(r => r.DownstreamPath, f => "/process")
            .RuleFor(r => r.DownstreamQueryString, f => string.Empty)
            .RuleFor(r => r.DownstreamRequestSize, f => f.Random.Long(100, 50000))
            .RuleFor(r => r.DownstreamResponseSize, f => f.Random.Long(200, 100000))
            .RuleFor(r => r.DownstreamStatusCode, f => f.PickRandom(_statusCodes))
            .RuleFor(r => r.GatewayLatencyMs, f => f.Random.Long(10, 5000))
            
            // Gateway
            .RuleFor(r => r.IsError, (f, r) => r.DownstreamStatusCode >= 400)
            .RuleFor(r => r.ErrorMessage, (f, r) => r.IsError ? f.Lorem.Sentence(5, 10) : null)
            .RuleFor(r => r.RequestBody, f => f.Random.Bool(0.5f) ? $"{{\"document_id\": \"{f.Random.Number(1000, 9999)}\", \"type\": \"invoice\"}}" : null);
    }

    public RequestLogEntry Generate()
    {
        return _faker.Generate();
    }

    public List<RequestLogEntry> Generate(int count)
    {
        return _faker.Generate(count);
    }
}
