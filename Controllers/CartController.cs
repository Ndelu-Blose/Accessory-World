using Microsoft.AspNetCore.Mvc;
using AccessoryWorld.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using AccessoryWorld.Security;
using AccessoryWorld.Models;

namespace AccessoryWorld.Controllers
{
    [Route("[controller]")]
    public class CartController(ICartService cartService, ISecurityValidationService securityValidation) : Controller
    {
        private readonly ICartService _cartService = cartService;
        private readonly ISecurityValidationService _securityValidation = securityValidation;
        
        [HttpPost("AddItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem([FromBody] AddToCartRequest request)
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                
                var success = await _cartService.AddToCartAsync(sessionId, request.ProductId, request.SKUId, request.Quantity, userId);
                
                if (success)
                {
                    var cartCount = await _cartService.GetCartItemCountAsync(sessionId, userId);
                    return Json(new { success = true, cartCount });
                }
                
                return Json(new { success = false, message = "Failed to add item to cart. Please check stock availability." });
            }
            catch (AccessoryWorld.Exceptions.DomainException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception here if you have logging configured
                return Json(new { success = false, message = "An unexpected error occurred while adding item to cart." });
            }
        }
        
        [HttpPost("UpdateItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                
                var success = await _cartService.UpdateCartItemAsync(sessionId, request.CartItemId, request.Quantity, userId);
                
                if (success)
                {
                    var cartCount = await _cartService.GetCartItemCountAsync(sessionId, userId);
                    return Json(new { success = true, message = "Item updated successfully.", cartCount });
                }
                
                return Json(new { success = false, message = "Failed to update item quantity." });
            }
            catch (DomainException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An unexpected error occurred while updating item quantity." });
            }
        }
        
        [HttpPost("RemoveItem")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemRequest request)
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var success = await _cartService.RemoveFromCartAsync(sessionId, request.CartItemId, userId);
            
            if (success)
            {
                var cartCount = await _cartService.GetCartItemCountAsync(sessionId, userId);
                return Json(new { success = true, message = "Item removed from cart successfully.", cartCount });
            }
            
            return Json(new { success = false, message = "Failed to remove item from cart." });
        }
        
        [HttpPost("Clear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var success = await _cartService.ClearCartAsync(sessionId, userId);
            
            if (success)
            {
                return Json(new { success = true, message = "Cart cleared successfully.", cartCount = 0 });
            }
            
            return Json(new { success = false, message = "Failed to clear cart." });
        }
        
        [HttpGet("GetCount")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetCount()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var count = await _cartService.GetCartItemCountAsync(sessionId, userId);
            
            return Json(new { count });
        }
        
        [HttpGet("GetCart")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetCart()
        {
            var sessionId = HttpContext.Session.Id;
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            
            var cart = await _cartService.GetCartAsync(sessionId, userId);
            
            return Json(new 
            {
                items = cart.Items.Select(item => new
                {
                    id = item.Id,
                    productId = item.ProductId,
                    productName = item.Product?.Name ?? "Unknown Product",
                    brandName = item.Product?.Brand?.Name ?? "Unknown Brand",
                    skuId = item.SKUId,
                    variant = item.SKU?.Variant ?? "Standard",
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    totalPrice = item.TotalPrice,
                    imageUrl = item.Product?.ProductImages?.FirstOrDefault()?.ImageUrl
                }),
                totalItems = cart.TotalItems,
                subTotal = cart.SubTotal,
                vatAmount = cart.VATAmount,
                total = cart.Total
            });
        }
    }
    
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int SKUId { get; set; }
        public int Quantity { get; set; } = 1;
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