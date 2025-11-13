using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Middleware
{
    /// <summary>
    /// Middleware to track active gateway requests and metrics
    /// </summary>
    public class RequestMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private static long _totalRequests = 0;
        private static int _activeRequests = 0;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public RequestMetricsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip tracking for internal endpoints
            var path = context.Request.Path.Value?.ToLowerInvariant();
            if (path != null && (
                path.StartsWith("/api/systemstatus") ||
                path.StartsWith("/account/") ||
                path.StartsWith("/user/") ||
                path.StartsWith("/dashboard") ||
                path.StartsWith("/_framework") ||
                path.Contains(".css") ||
                path.Contains(".js") ||
                path.Contains(".map")))
            {
                await _next(context);
                return;
            }

            // Increment counters
            Interlocked.Increment(ref _totalRequests);
            Interlocked.Increment(ref _activeRequests);

            try
            {
                await _next(context);
            }
            finally
            {
                // Decrement active counter when request completes
                Interlocked.Decrement(ref _activeRequests);
            }
        }

        /// <summary>
        /// Get current metrics snapshot
        /// </summary>
        public static GatewayMetrics GetMetrics()
        {
            return new GatewayMetrics
            {
                StartTime = _startTime,
                TotalRequests = _totalRequests,
                ActiveRequests = _activeRequests,
                UptimeSeconds = (long)(DateTime.UtcNow - _startTime).TotalSeconds
            };
        }

        /// <summary>
        /// Reset counters (for testing purposes)
        /// </summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref _totalRequests, 0);
            Interlocked.Exchange(ref _activeRequests, 0);
        }
    }

    public class GatewayMetrics
    {
        public DateTime StartTime { get; set; }
        public long TotalRequests { get; set; }
        public int ActiveRequests { get; set; }
        public long UptimeSeconds { get; set; }
    }
}
