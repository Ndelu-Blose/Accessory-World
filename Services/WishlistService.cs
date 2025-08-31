using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessoryWorld.Services
{
    public interface IWishlistService
    {
        Task<bool> AddToWishlistAsync(string userId, int productId);
        Task<bool> RemoveFromWishlistAsync(string userId, int productId);
        Task<bool> IsInWishlistAsync(string userId, int productId);
        Task<List<WishlistItem>> GetWishlistItemsAsync(string userId);
        Task<int> GetWishlistCountAsync(string userId);
        Task<bool> ClearWishlistAsync(string userId);
    }

    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddToWishlistAsync(string userId, int productId)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return false;

                // Check if already in wishlist
                var existingItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existingItem != null)
                    return false; // Already in wishlist

                // Add to wishlist
                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlistItem);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int productId)
        {
            try
            {
                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem == null)
                    return false;

                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsInWishlistAsync(string userId, int productId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task<List<WishlistItem>> GetWishlistItemsAsync(string userId)
        {
            return await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .ThenInclude(p => p.ProductImages)
                .Select(w => new WishlistItem
                {
                    Id = w.Id,
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    ProductImage = w.Product.ProductImages.FirstOrDefault() != null ? w.Product.ProductImages.First().ImageUrl : "/images/no-image.jpg",
                    Price = w.Product.Price,
                    Currency = "ZAR",
                    IsInStock = w.Product.InStock,
                    AddedAt = w.CreatedAt,
                    ProductUrl = $"/Products/Details/{w.ProductId}"
                })
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.Wishlists
                .CountAsync(w => w.UserId == userId);
        }

        public async Task<bool> ClearWishlistAsync(string userId)
        {
            try
            {
                var wishlistItems = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                if (wishlistItems.Any())
                {
                    _context.Wishlists.RemoveRange(wishlistItems);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}