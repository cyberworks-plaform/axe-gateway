
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Repositories;
using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Configuration;

namespace Ce.Gateway.Api.Middleware
{
    public class RequestLoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogWriter _logWriter;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestLoggingDelegatingHandler(ILogWriter logWriter, IHttpContextAccessor httpContextAccessor)
        {
            _logWriter = logWriter;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = _httpContextAccessor.HttpContext;
            // DownstreamRoute can be null in some case, such as 404 Not Found.
            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = null;
            string error = null;
            int statusCode = 500; // Default to 500 in case of exception before response is received

            try
            {
                response = await base.SendAsync(request, cancellationToken);
                statusCode = (int)response.StatusCode;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                // rethrow the exception to let Ocelot handle it
                throw;
            }
            finally
            {
                stopwatch.Stop();

                string downstreamNode = "unknown";
                string route = "unknown";
                string serviceApi = "unknown";

                if (request.RequestUri != null)
                {
                    downstreamNode = $"{request.RequestUri.Scheme}://{request.RequestUri.Host}:{request.RequestUri.Port}";
                    route = request.RequestUri.AbsolutePath;
                    serviceApi = request.RequestUri.Host;
                }

                var logEntry = new RequestLogEntry
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier,
                    Route = route,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    DownstreamNode = downstreamNode,
                    StatusCode = statusCode,
                    LatencyMs = stopwatch.ElapsedMilliseconds,
                    ServiceApi = serviceApi,
                    Client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    RequestSize = context.Request.ContentLength ?? 0,
                    ResponseSize = response?.Content?.Headers?.ContentLength ?? 0,
                    Error = error ?? (context.Items.Errors().Any() ? string.Join("; ", context.Items.Errors().Select(e => e.Message)) : null)
                };

                // Fire and forget
                _ = _logWriter.WriteLogAsync(logEntry);
            }

            return response;
        }
    }
}
