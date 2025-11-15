using System;
namespace Ce.Gateway.Api.Models.RouteConfig;

/// <summary>
/// Quality of Service configuration options
/// </summary>
public class QoSOptionsDto
{
    /// <summary>
    /// Timeout value in milliseconds
    /// </summary>
    public int? TimeoutValue { get; set; }

    /// <summary>
    /// Number of exceptions allowed before breaking the circuit
    /// </summary>
    public int? ExceptionsAllowedBeforeBreaking { get; set; }

    /// <summary>
    /// Duration of circuit break in milliseconds
    /// </summary>
    public int? DurationOfBreak { get; set; }
}
