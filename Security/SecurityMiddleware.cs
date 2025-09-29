using AccessoryWorld.Services;
using System.Text;

namespace AccessoryWorld.Security
{
    public class SecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityMiddleware> _logger;
        private readonly SecurityMiddlewareOptions _options;
        
        public SecurityMiddleware(RequestDelegate next, ILogger<SecurityMiddleware> logger, SecurityMiddlewareOptions options)
        {
            _next = next;
            _logger = logger;
            _options = options;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            AddSecurityHeaders(context);
            
            // Validate request size
            if (context.Request.ContentLength > _options.MaxRequestSize)
            {
                _logger.LogWarning("Request size {Size} exceeds limit {Limit} from IP {IP}", 
                    context.Request.ContentLength, _options.MaxRequestSize, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsync("Request too large");
                return;
            }
            
            // Rate limiting check (basic implementation)
            if (!await CheckRateLimit(context))
            {
                _logger.LogWarning("Rate limit exceeded for IP {IP}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 429; // Too Many Requests
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }
            
            // Validate user agent (block known bad bots)
            if (IsBlockedUserAgent(context.Request.Headers.UserAgent.ToString()))
            {
                _logger.LogWarning("Blocked user agent {UserAgent} from IP {IP}", 
                    context.Request.Headers.UserAgent, context.Connection.RemoteIpAddress);
                context.Response.StatusCode = 403; // Forbidden
                await context.Response.WriteAsync("Access denied");
                return;
            }
            
            await _next(context);
        }
        
        private static void AddSecurityHeaders(HttpContext context)
        {
            var response = context.Response;
            
            // Prevent clickjacking
            response.Headers.Append("X-Frame-Options", "DENY");
            
            // Prevent MIME type sniffing
            response.Headers.Append("X-Content-Type-Options", "nosniff");
            
            // Enable XSS protection
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
            
            // Referrer policy
            response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            
            // Content Security Policy
            response.Headers.Append("Content-Security-Policy", 
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
                "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none';");
            
            // Strict Transport Security (only in production with HTTPS)
            if (!context.Request.Host.Host.Contains("localhost"))
            {
                response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }
        }
        
        private Task<bool> CheckRateLimit(HttpContext context)
        {
            // Simple in-memory rate limiting (in production, use Redis or similar)
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"rate_limit_{clientIp}";
            
            return Task.FromResult(true); // Allow all requests for now - rate limiting disabled for development
        }
        
        private static bool IsBlockedUserAgent(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return true; // Block empty user agents
            
            var blockedPatterns = new[]
            {
                "sqlmap", "nikto", "nmap", "masscan", "zap", "burp",
                "bot", "crawler", "spider", "scraper"
            };
            
            var lowerUserAgent = userAgent.ToLowerInvariant();
            return blockedPatterns.Any(pattern => lowerUserAgent.Contains(pattern));
        }
    }
    
    public class SecurityMiddlewareOptions
    {
        public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB
        public int RateLimitRequests { get; set; } = 100;
        public TimeSpan RateLimitWindow { get; set; } = TimeSpan.FromMinutes(1);
        public bool EnableRequestLogging { get; set; } = true;
    }
    
    public static class SecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder builder, 
            Action<SecurityMiddlewareOptions>? configureOptions = null)
        {
            var options = new SecurityMiddlewareOptions();
            configureOptions?.Invoke(options);
            
            return builder.UseMiddleware<SecurityMiddleware>(options);
        }
    }
}