using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO; // Added for StreamReader
using Ce.Gateway.Api.Entities;
using Microsoft.AspNetCore.Http;
using Ocelot.Middleware;
using Ce.Gateway.Api.Repositories.Interface; // Add this using statement
using Ce.Gateway.Api.Utilities; // Added for BufferingStream

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

            // Store original request content before it might be replaced
            HttpContent originalRequestContent = request.Content;

            // Wrap the original request body stream in our BufferingStream
            var originalBodyStream = context.Request.Body;
            var bufferedBodyStream = new BufferingStream(originalBodyStream);
            context.Request.Body = bufferedBodyStream; // Replace the request body stream with our buffering stream

            string requestBody = null;

            // Only read if there's content and it's a POST/PUT/PATCH
            if (context.Request.ContentLength > 0 && (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch))
            {
                // Read from the buffered stream for logging
                bufferedBodyStream.Position = 0; // Ensure position is at the beginning for logging
                using (var reader = new StreamReader(bufferedBodyStream, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                }
                bufferedBodyStream.Position = 0; // Reset position for downstream processing

                // Explicitly set request.Content for downstream with the buffered stream.
                // This is the least intrusive way to ensure the downstream request gets the buffered content.
                // We create a new StreamContent that wraps the already buffered stream.
                request.Content = new StreamContent(bufferedBodyStream);

                // Copy headers from original content to the new content
                if (originalRequestContent != null)
                {
                    foreach (var header in originalRequestContent.Headers)
                    {
                        request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            }

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

                // Capture error from response body if not successful
                if (response != null && !response.IsSuccessStatusCode && response.Content != null)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        // Append response body to existing error or set it as the error
                        error = string.IsNullOrWhiteSpace(error) ? responseBody : $"{error}; Response Body: {responseBody}";
                    }
                }

                var logEntry = new RequestLogEntry
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow,
                    TraceId = context.TraceIdentifier,

                    // Upstream Information
                    UpstreamHost = context.Request.Host.Host,
                    UpstreamPort = context.Request.Host.Port,
                    UpstreamScheme = context.Request.Scheme,
                    UpstreamHttpMethod = context.Request.Method,
                    UpstreamPath = context.Request.Path,
                    UpstreamQueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                    UpstreamRequestSize = context.Request.ContentLength,
                    UpstreamClientIp = context.Connection.RemoteIpAddress?.ToString(),

                    // Downstream Information
                    DownstreamScheme = request.RequestUri?.Scheme,
                    DownstreamHost = request.RequestUri?.Host,
                    DownstreamPort = request.RequestUri?.Port,
                    DownstreamPath = request.RequestUri?.AbsolutePath,
                    DownstreamQueryString = request.RequestUri?.Query,
                    DownstreamRequestSize = request.Content?.Headers?.ContentLength,
                    DownstreamResponseSize = response?.Content?.Headers?.ContentLength,
                    DownstreamStatusCode = (int?)response?.StatusCode,

                    // Gateway Information
                    GatewayLatencyMs = stopwatch.ElapsedMilliseconds,
                    IsError = error != null || (response != null && !response.IsSuccessStatusCode) || context.Items.Errors().Any(), // Update IsError logic
                    ErrorMessage = error ?? (context.Items.Errors().Any() ? string.Join("; ", context.Items.Errors().Select(e => e.Message)) : null),
                    RequestBody = requestBody
                };

                // Fire and forget
                _ = _logWriter.WriteLogAsync(logEntry);
            }

            return response;
        }
    }
}