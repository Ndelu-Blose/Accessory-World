using AccessoryWorld.Models;

namespace AccessoryWorld.Services
{
    public interface ICartService
    {
        Task<bool> AddToCartAsync(string sessionId, int productId, int skuId, int quantity, string? userId = null);
        Task<bool> UpdateCartItemAsync(string sessionId, int cartItemId, int quantity, string? userId = null);
        Task<bool> RemoveFromCartAsync(string sessionId, int cartItemId, string? userId = null);
        Task<bool> ClearCartAsync(string sessionId, string? userId = null);
        Task<Cart> GetCartAsync(string sessionId, string? userId = null);
        Task<int> GetCartItemCountAsync(string sessionId, string? userId = null);
        Task<bool> MergeGuestCartAsync(string sessionId, string userId);
        Task<decimal> GetCartTotalAsync(string sessionId, string? userId = null);
    }
}