using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccessoryWorld.Services
{
    public interface IRecommendationService
    {
        Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int count = 6);
        Task<List<Product>> GetSimilarProductsAsync(int productId, int count = 4);
        Task<List<Product>> GetTrendingProductsAsync(int count = 6);
    }

    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(ApplicationDbContext context, ILogger<RecommendationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Product>> GetPersonalizedRecommendationsAsync(string userId, int count = 6)
        {
            try
            {
                // Get user's order history
                var userOrders = await _context.Orders
                    .Where(o => o.UserId == userId && o.Status != "CANCELLED")
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                    .ThenInclude(s => s.Product)
                    .ThenInclude(p => p.Category)
                    .ToListAsync();

                if (!userOrders.Any())
                {
                    // New user - return trending products
                    return await GetTrendingProductsAsync(count);
                }

                // Get categories and brands from user's purchase history
                var purchasedCategoryIds = userOrders
                    .SelectMany(o => o.OrderItems)
                    .Select(oi => oi.SKU.Product.CategoryId)
                    .Distinct()
                    .ToList();

                var purchasedBrandIds = userOrders
                    .SelectMany(o => o.OrderItems)
                    .Select(oi => oi.SKU.Product.BrandId)
                    .Distinct()
                    .ToList();

                var purchasedProductIds = userOrders
                    .SelectMany(o => o.OrderItems)
                    .Select(oi => oi.SKU.Product.Id)
                    .Distinct()
                    .ToList();

                // Get recommendations based on purchase history
                var recommendations = await _context.Products
                    .Where(p => p.IsActive && !purchasedProductIds.Contains(p.Id))
                    .Where(p => purchasedCategoryIds.Contains(p.CategoryId) || purchasedBrandIds.Contains(p.BrandId))
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.SalesCount)
                    .ThenByDescending(p => p.IsBestSeller)
                    .ThenByDescending(p => p.IsNew)
                    .Take(count)
                    .ToListAsync();

                // If not enough recommendations from purchase history, fill with trending products
                if (recommendations.Count < count)
                {
                    var additionalCount = count - recommendations.Count;
                    var trendingProducts = await GetTrendingProductsAsync(additionalCount * 2);
                    
                    var additionalRecommendations = trendingProducts
                        .Where(p => !recommendations.Any(r => r.Id == p.Id) && !purchasedProductIds.Contains(p.Id))
                        .Take(additionalCount)
                        .ToList();
                    
                    recommendations.AddRange(additionalRecommendations);
                }

                return recommendations.Take(count).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personalized recommendations for user {UserId}", userId);
                return await GetTrendingProductsAsync(count);
            }
        }

        public async Task<List<Product>> GetSimilarProductsAsync(int productId, int count = 4)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product == null)
                {
                    return new List<Product>();
                }

                // Find similar products in same category or brand
                var similarProducts = await _context.Products
                    .Where(p => p.IsActive && p.Id != productId)
                    .Where(p => p.CategoryId == product.CategoryId || p.BrandId == product.BrandId)
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.CategoryId == product.CategoryId ? 2 : 1) // Prioritize same category
                    .ThenByDescending(p => p.SalesCount)
                    .Take(count)
                    .ToListAsync();

                return similarProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products for product {ProductId}", productId);
                return new List<Product>();
            }
        }

        public async Task<List<Product>> GetTrendingProductsAsync(int count = 6)
        {
            try
            {
                var trendingProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.SalesCount)
                    .ThenByDescending(p => p.IsBestSeller)
                    .ThenByDescending(p => p.IsNew)
                    .ThenByDescending(p => p.IsHot)
                    .Take(count)
                    .ToListAsync();

                return trendingProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trending products");
                return new List<Product>();
            }
        }
    }
}