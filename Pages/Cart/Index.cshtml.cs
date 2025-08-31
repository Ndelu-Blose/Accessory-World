using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AccessoryWorld.Services;
using AccessoryWorld.Models;
using System.Security.Claims;

namespace AccessoryWorld.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;
        
        public IndexModel(ICartService cartService)
        {
            _cartService = cartService;
        }
        
        public AccessoryWorld.Models.Cart Cart { get; set; } = new AccessoryWorld.Models.Cart();
        
        public async Task OnGetAsync()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            Cart = await _cartService.GetCartAsync(sessionId, userId);
        }
        
        public async Task<IActionResult> OnPostUpdateItemAsync([FromBody] UpdateCartItemRequest request)
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var success = await _cartService.UpdateCartItemAsync(sessionId, request.CartItemId, request.Quantity, userId);
            
            if (success)
            {
                return new JsonResult(new { success = true });
            }
            
            return new JsonResult(new { success = false, message = "Failed to update item" });
        }
        
        public async Task<IActionResult> OnPostRemoveItemAsync([FromBody] RemoveCartItemRequest request)
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var success = await _cartService.RemoveFromCartAsync(sessionId, request.CartItemId, userId);
            
            if (success)
            {
                return new JsonResult(new { success = true });
            }
            
            return new JsonResult(new { success = false, message = "Failed to remove item" });
        }
        
        public async Task<IActionResult> OnPostClearAsync()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var success = await _cartService.ClearCartAsync(sessionId, userId);
            
            if (success)
            {
                return new JsonResult(new { success = true });
            }
            
            return new JsonResult(new { success = false, message = "Failed to clear cart" });
        }
        
        public async Task<IActionResult> OnGetCountAsync()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var count = await _cartService.GetCartItemCountAsync(sessionId, userId);
            
            return new JsonResult(new { count });
        }
    }
    
    public class UpdateCartItemRequest
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }
    
    public class RemoveCartItemRequest
    {
        public int CartItemId { get; set; }
    }
}