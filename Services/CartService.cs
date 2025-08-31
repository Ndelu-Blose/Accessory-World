using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessoryWorld.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartAsync(string sessionId, string? userId = null);
        Task<bool> AddToCartAsync(string sessionId, int productId, int skuId, int quantity, string? userId = null);
        Task<bool> UpdateCartItemAsync(string sessionId, int cartItemId, int quantity, string? userId = null);
        Task<bool> RemoveFromCartAsync(string sessionId, int cartItemId, string? userId = null);
        Task<bool> ClearCartAsync(string sessionId, string? userId = null);
        Task<int> GetCartItemCountAsync(string sessionId, string? userId = null);
        Task MergeCartsAsync(string sessionId, string userId);
    }
    
    public class CartService(ApplicationDbContext context) : ICartService
    {
        private readonly ApplicationDbContext _context = context;
        
        public async Task<Cart> GetCartAsync(string sessionId, string? userId = null)
        {
            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Brand)
                .Include(ci => ci.Product)
                    .ThenInclude(p => p.Category)
                .Include(ci => ci.SKU)
                .Where(ci => ci.SessionId == sessionId || (userId != null && ci.UserId == userId))
                .ToListAsync();
            
            return new Cart
            {
                SessionId = sessionId,
                UserId = userId,
                Items = cartItems
            };
        }
        
        public async Task<bool> AddToCartAsync(string sessionId, int productId, int skuId, int quantity, string? userId = null)
        {
            // Validate input parameters
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero", DomainErrors.INVALID_QUANTITY);
            
            if (quantity > 100)
                throw new DomainException("Cannot add more than 100 items at once", DomainErrors.INVALID_QUANTITY);
            
            // Check if the product and SKU exist and are active
            var sku = await _context.SKUs
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == skuId && s.ProductId == productId);
            
            if (sku == null)
                throw new DomainException("Product or SKU not found", DomainErrors.PRODUCT_NOT_FOUND);
            
            if (sku.Product?.IsActive != true)
                throw new DomainException("Product is not available", DomainErrors.PRODUCT_INACTIVE);
            
            // Check available stock (considering reserved quantity)
            var availableStock = sku.StockQuantity - sku.ReservedQuantity;
            if (availableStock < quantity)
                throw new DomainException($"Insufficient stock. Available: {availableStock}, Requested: {quantity}", DomainErrors.INSUFFICIENT_STOCK);
            
            // Check if item already exists in cart
            var existingCartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => 
                    (ci.SessionId == sessionId || (userId != null && ci.UserId == userId)) &&
                    ci.ProductId == productId && 
                    ci.SKUId == skuId);
            
            if (existingCartItem != null)
            {
                // Update quantity if item already exists
                var newQuantity = existingCartItem.Quantity + quantity;
                if (newQuantity > availableStock)
                    throw new DomainException($"Insufficient stock for total quantity. Available: {availableStock}, Total requested: {newQuantity}", DomainErrors.INSUFFICIENT_STOCK);
                
                if (newQuantity > 100)
                    throw new DomainException("Cannot have more than 100 items of the same product in cart", DomainErrors.INVALID_QUANTITY);
                
                existingCartItem.Quantity = newQuantity;
                existingCartItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new cart item
                var currentPrice = sku.Product?.IsOnSale == true && sku.Product.SalePrice.HasValue 
                    ? sku.Product.SalePrice.Value 
                    : sku.Product?.Price ?? 0m;
                
                var cartItem = new CartItem
                {
                    SessionId = sessionId,
                    UserId = userId,
                    ProductId = productId,
                    SKUId = skuId,
                    Quantity = quantity,
                    UnitPrice = currentPrice
                };
                
                _context.CartItems.Add(cartItem);
            }
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> UpdateCartItemAsync(string sessionId, int cartItemId, int quantity, string? userId = null)
        {
            // Validate input parameters
            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero", DomainErrors.INVALID_QUANTITY);
            
            if (quantity > 100)
                throw new DomainException("Cannot have more than 100 items of the same product in cart", DomainErrors.INVALID_QUANTITY);
            
            var cartItem = await _context.CartItems
                .Include(ci => ci.SKU)
                .ThenInclude(s => s.Product)
                .FirstOrDefaultAsync(ci => 
                    ci.Id == cartItemId && 
                    (ci.SessionId == sessionId || (userId != null && ci.UserId == userId)));
            
            if (cartItem == null)
                throw new DomainException("Cart item not found", DomainErrors.CART_ITEM_NOT_FOUND);
            
            if (cartItem.SKU?.Product?.IsActive != true)
                throw new DomainException("Product is no longer available", DomainErrors.PRODUCT_INACTIVE);
            
            // Check available stock (considering reserved quantity)
            var availableStock = cartItem.SKU.StockQuantity - cartItem.SKU.ReservedQuantity;
            if (availableStock < quantity)
                throw new DomainException($"Insufficient stock. Available: {availableStock}, Requested: {quantity}", DomainErrors.INSUFFICIENT_STOCK);
            
            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> RemoveFromCartAsync(string sessionId, int cartItemId, string? userId = null)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => 
                        ci.Id == cartItemId && 
                        (ci.SessionId == sessionId || (userId != null && ci.UserId == userId)));
                
                if (cartItem == null)
                {
                    return false;
                }
                
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<bool> ClearCartAsync(string sessionId, string? userId = null)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Where(ci => ci.SessionId == sessionId || (userId != null && ci.UserId == userId))
                    .ToListAsync();
                
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        public async Task<int> GetCartItemCountAsync(string sessionId, string? userId = null)
        {
            return await _context.CartItems
                .Where(ci => ci.SessionId == sessionId || (userId != null && ci.UserId == userId))
                .SumAsync(ci => ci.Quantity);
        }
        
        public async Task MergeCartsAsync(string sessionId, string userId)
        {
            try
            {
                // Get session cart items
                var sessionCartItems = await _context.CartItems
                    .Where(ci => ci.SessionId == sessionId && ci.UserId == null)
                    .ToListAsync();
                
                // Update session cart items to be associated with the user
                foreach (var item in sessionCartItems)
                {
                    // Check if user already has this item in their cart
                    var existingUserItem = await _context.CartItems
                        .FirstOrDefaultAsync(ci => 
                            ci.UserId == userId && 
                            ci.ProductId == item.ProductId && 
                            ci.SKUId == item.SKUId);
                    
                    if (existingUserItem != null)
                    {
                        // Merge quantities
                        existingUserItem.Quantity += item.Quantity;
                        existingUserItem.UpdatedAt = DateTime.UtcNow;
                        _context.CartItems.Remove(item);
                    }
                    else
                    {
                        // Associate session item with user
                        item.UserId = userId;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }
                
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Log error but don't throw - cart merge is not critical
            }
        }
    }
}