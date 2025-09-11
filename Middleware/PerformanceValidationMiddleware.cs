using AccessoryWorld.Exceptions;
using AccessoryWorld.Services;
using Microsoft.Extensions.Logging;

namespace AccessoryWorld.Middleware
{
    public class PerformanceValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IPerformanceValidationService _performanceValidation;
        private readonly ILogger<PerformanceValidationMiddleware> _logger;

        public PerformanceValidationMiddleware(
            RequestDelegate next,
            IPerformanceValidationService performanceValidation,
            ILogger<PerformanceValidationMiddleware> logger)
        {
            _next = next;
            _performanceValidation = performanceValidation;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = Guid.NewGuid().ToString();
            var clientId = GetClientId(context);
            var endpoint = $"{context.Request.Method} {context.Request.Path}";

            try
            {
                // Validate rate limits
                _performanceValidation.ValidateRateLimit(clientId, endpoint);
                
                // Validate concurrency limits
                _performanceValidation.ValidateConcurrencyLimit(clientId);
                
                // Record request start
                _performanceValidation.RecordRequestStart(requestId, clientId);

                // Execute request with timeout
                await _performanceValidation.ExecuteWithTimeoutAsync(async () =>
                {
                    await _next(context);
                }, 30); // 30 second timeout for HTTP requests
            }
            catch (DomainException ex) when (ex.ErrorCode == DomainErrors.SYSTEM_ERROR)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync(ex.Message);
            }
            finally
            {
                // Record request end
                _performanceValidation.RecordRequestEnd(requestId);
            }
        }

        private string GetClientId(HttpContext context)
        {
            // Try to get client ID from various sources
            var clientId = context.User?.Identity?.Name;
            
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
            }
            
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            return clientId;
        }
    }
}