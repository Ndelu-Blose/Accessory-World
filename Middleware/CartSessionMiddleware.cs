using AccessoryWorld.Services;
using System.Security.Claims;

namespace AccessoryWorld.Middleware
{
    public class CartSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CartSessionMiddleware> _logger;

        public CartSessionMiddleware(RequestDelegate next, ILogger<CartSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ICartService cartService)
        {
            // Check if user just logged in (has authentication but no cart merge flag)
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionId = context.Session.Id;
                
                // Check if we need to merge guest cart with user cart
                var cartMergedKey = $"cart_merged_{userId}";
                if (!context.Session.Keys.Contains(cartMergedKey))
                {
                    try
                    {
                        // Merge guest cart with user cart
                        await cartService.MergeGuestCartAsync(sessionId, userId);
                        
                        // Mark as merged to avoid duplicate merges
                        context.Session.SetString(cartMergedKey, "true");
                        
                        _logger.LogInformation("Cart merged for user {UserId} from session {SessionId}", userId, sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to merge cart for user {UserId}", userId);
                        // Don't throw - cart merge failure shouldn't break the request
                    }
                }
            }

            await _next(context);
        }
    }

    public static class CartSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCartSessionManagement(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CartSessionMiddleware>();
        }
    }
}