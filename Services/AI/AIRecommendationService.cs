using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Models.AI;
using AccessoryWorld.DTOs.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace AccessoryWorld.Services.AI
{
    public interface IAIRecommendationService
    {
        Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request);
        Task<SimilarProductResponse> GetSimilarProductsAsync(SimilarProductRequest request);
        Task TrackUserBehaviorAsync(UserBehaviorRequest request);
        Task RecordRecommendationFeedbackAsync(RecommendationFeedbackRequest request);
        Task<UserProfileResponse> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(UserProfileRequest request);
        Task<List<RecommendationMetrics>> GetRecommendationMetricsAsync(DateTime fromDate, DateTime toDate);
        Task<ABTestResponse> AssignUserToTestGroupAsync(string userId, string testName);
    }

    public class AIRecommendationService : IAIRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AIRecommendationService> _logger;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public AIRecommendationService(
            ApplicationDbContext context,
            ILogger<AIRecommendationService> logger,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var algorithmType = request.AlgorithmType ?? "HYBRID";
                var cacheKey = $"recommendations_{request.UserId}_{algorithmType}_{request.Count}_{string.Join(",", request.ExcludeProductIds ?? new List<string>())}";
                
                if (_cache.TryGetValue(cacheKey, out RecommendationResponse? cachedResponse))
                {
                    return cachedResponse!;
                }

                var response = new RecommendationResponse
                {
                    AlgorithmUsed = algorithmType,
                    TestGroup = request.TestGroup ?? "default"
                };

                List<RecommendedProduct> recommendations;

                switch (algorithmType.ToUpperInvariant())
                {
                    case "COLLABORATIVE":
                        recommendations = await GetCollaborativeRecommendationsAsync(request);
                        break;
                    case "CONTENT_BASED":
                        recommendations = await GetContentBasedRecommendationsAsync(request);
                        break;
                    case "TRENDING":
                        recommendations = await GetTrendingRecommendationsAsync(request);
                        break;
                    case "HYBRID":
                    default:
                        recommendations = await GetHybridRecommendationsAsync(request);
                        break;
                }

                response.Products = recommendations;
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                response.TotalAvailable = recommendations.Count;

                // Cache for 5 minutes
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recommendations for user {UserId}", request.UserId);
                return new RecommendationResponse
                {
                    AlgorithmUsed = request.AlgorithmType ?? "FALLBACK",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Products = await GetFallbackRecommendationsAsync(request)
                };
            }
        }

        private async Task<List<RecommendedProduct>> GetCollaborativeRecommendationsAsync(RecommendationRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return await GetTrendingRecommendationsAsync(request);
            }

            // Get users with similar purchase patterns
            var similarUsers = await GetSimilarUsersAsync(request.UserId);
            
            // Get products liked by similar users
            var similarUserProductIds = await _context.UserBehaviors
                .Where(ub => similarUsers.Contains(ub.UserId) && 
                            (ub.ActionType == "PURCHASE" || ub.ActionType == "ADD_TO_CART"))
                .Select(ub => ub.ProductId)
                .Distinct()
                .ToListAsync();

            // Get products not yet purchased by current user
            var userPurchasedProducts = await _context.UserBehaviors
                .Where(ub => ub.UserId == request.UserId && ub.ActionType == "PURCHASE")
                .Select(ub => ub.ProductId)
                .ToListAsync();

            var candidateProducts = similarUserProductIds
                .Where(pid => !userPurchasedProducts.Contains(pid))
                .ToList();

            return await BuildRecommendationProductsAsync(candidateProducts, request, "COLLABORATIVE");
        }

        private async Task<List<RecommendedProduct>> GetContentBasedRecommendationsAsync(RecommendationRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return await GetTrendingRecommendationsAsync(request);
            }

            // Get user's preferred categories and brands
            var userProfile = await GetUserProfileAsync(request.UserId);
            var userPreferences = await GetUserPreferencesAsync(request.UserId);

            var query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            // Filter by user preferences
            if (userPreferences.Categories.Any())
            {
                query = query.Where(p => userPreferences.Categories.Contains(p.Category.Name));
            }

            if (userPreferences.Brands.Any())
            {
                query = query.Where(p => userPreferences.Brands.Contains(p.Brand.Name));
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= request.MinPrice.Value);
            }

            var products = await query
                .OrderByDescending(p => p.SalesCount)
                .ThenByDescending(p => p.IsBestSeller)
                .Take(request.Count * 2) // Get more to filter and rank
                .ToListAsync();

            return await BuildRecommendationProductsAsync(products.Select(p => p.Id).ToList(), request, "CONTENT_BASED");
        }

        private async Task<List<RecommendedProduct>> GetTrendingRecommendationsAsync(RecommendationRequest request)
        {
            var query = _context.Products
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= request.MaxPrice.Value);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= request.MinPrice.Value);
            }

            var products = await query
                .OrderByDescending(p => p.SalesCount)
                .ThenByDescending(p => p.IsBestSeller)
                .ThenByDescending(p => p.IsNew)
                .ThenByDescending(p => p.IsHot)
                .Take(request.Count)
                .ToListAsync();

            return await BuildRecommendationProductsAsync(products.Select(p => p.Id).ToList(), request, "TRENDING");
        }

        private async Task<List<RecommendedProduct>> GetHybridRecommendationsAsync(RecommendationRequest request)
        {
            var collaborative = await GetCollaborativeRecommendationsAsync(request);
            var contentBased = await GetContentBasedRecommendationsAsync(request);
            var trending = await GetTrendingRecommendationsAsync(request);

            // Combine and re-rank recommendations
            var allRecommendations = new List<RecommendedProduct>();
            
            // Weight collaborative filtering more for users with purchase history
            var hasPurchaseHistory = !string.IsNullOrEmpty(request.UserId) && 
                await _context.UserBehaviors.AnyAsync(ub => ub.UserId == request.UserId && ub.ActionType == "PURCHASE");

            if (hasPurchaseHistory)
            {
                allRecommendations.AddRange(collaborative.Take(request.Count / 2));
                allRecommendations.AddRange(contentBased.Take(request.Count / 3));
                allRecommendations.AddRange(trending.Take(request.Count / 6));
            }
            else
            {
                allRecommendations.AddRange(trending.Take(request.Count / 2));
                allRecommendations.AddRange(contentBased.Take(request.Count / 2));
            }

            // Remove duplicates and re-rank
            var uniqueRecommendations = allRecommendations
                .GroupBy(r => r.ProductId)
                .Select(g => g.OrderByDescending(r => r.RecommendationScore).First())
                .OrderByDescending(r => r.RecommendationScore)
                .Take(request.Count)
                .ToList();

            // Update ranks
            for (int i = 0; i < uniqueRecommendations.Count; i++)
            {
                uniqueRecommendations[i].Rank = i + 1;
            }

            return uniqueRecommendations;
        }

        private async Task<List<RecommendedProduct>> GetFallbackRecommendationsAsync(RecommendationRequest request)
        {
            return await GetTrendingRecommendationsAsync(request);
        }

        private async Task<List<RecommendedProduct>> BuildRecommendationProductsAsync(
            List<int> productIds, 
            RecommendationRequest request, 
            string algorithmType)
        {
            if (!productIds.Any())
            {
                return new List<RecommendedProduct>();
            }

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();

            var excludeIds = request.ExcludeProductIds?.Select(int.Parse).ToList() ?? new List<int>();
            
            var recommendations = products
                .Where(p => !excludeIds.Contains(p.Id))
                .Select((p, index) => new RecommendedProduct
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    SalePrice = p.SalePrice,
                    ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
                    Brand = p.Brand.Name,
                    Category = p.Category.Name,
                    RecommendationScore = CalculateRecommendationScore(p, algorithmType),
                    Rank = index + 1,
                    Reason = GetRecommendationReason(p, algorithmType),
                    IsInStock = p.InStock,
                    IsOnSale = p.IsOnSale,
                    IsNew = p.IsNew,
                    IsBestSeller = p.IsBestSeller,
                    Tags = !string.IsNullOrEmpty(p.Tags) ? JsonSerializer.Deserialize<List<string>>(p.Tags) ?? new List<string>() : new List<string>()
                })
                .OrderByDescending(r => r.RecommendationScore)
                .Take(request.Count)
                .ToList();

            return recommendations;
        }

        private double CalculateRecommendationScore(Product product, string algorithmType)
        {
            var score = 0.0;

            // Base score from product popularity
            score += Math.Min(product.SalesCount / 100.0, 1.0) * 0.3;
            score += product.ViewCount / 1000.0 * 0.1;

            // Boost for special flags
            if (product.IsBestSeller) score += 0.2;
            if (product.IsNew) score += 0.15;
            if (product.IsHot) score += 0.1;
            if (product.IsOnSale) score += 0.05;

            // Algorithm-specific adjustments
            switch (algorithmType)
            {
                case "COLLABORATIVE":
                    score += 0.1; // Slight boost for collaborative filtering
                    break;
                case "CONTENT_BASED":
                    score += 0.05; // Slight boost for content-based
                    break;
                case "TRENDING":
                    score += 0.2; // Higher boost for trending
                    break;
            }

            return Math.Min(score, 1.0);
        }

        private string GetRecommendationReason(Product product, string algorithmType)
        {
            var reasons = new List<string>();

            if (product.IsBestSeller) reasons.Add("Best seller");
            if (product.IsNew) reasons.Add("New arrival");
            if (product.IsHot) reasons.Add("Trending");
            if (product.IsOnSale) reasons.Add("On sale");

            switch (algorithmType)
            {
                case "COLLABORATIVE":
                    reasons.Add("Similar customers also bought");
                    break;
                case "CONTENT_BASED":
                    reasons.Add("Matches your preferences");
                    break;
                case "TRENDING":
                    reasons.Add("Popular choice");
                    break;
                case "HYBRID":
                    reasons.Add("Recommended for you");
                    break;
            }

            return string.Join(", ", reasons);
        }

        private async Task<List<string>> GetSimilarUsersAsync(string userId)
        {
            // Get users who purchased similar products
            var userPurchases = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && ub.ActionType == "PURCHASE")
                .Select(ub => ub.ProductId)
                .ToListAsync();

            if (!userPurchases.Any())
            {
                return new List<string>();
            }

            var similarUsers = await _context.UserBehaviors
                .Where(ub => ub.ActionType == "PURCHASE" && 
                            ub.UserId != userId && 
                            userPurchases.Contains(ub.ProductId))
                .GroupBy(ub => ub.UserId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(10)
                .ToListAsync();

            return similarUsers;
        }

        private async Task<(List<string> Categories, List<string> Brands)> GetUserPreferencesAsync(string userId)
        {
            var behaviors = await _context.UserBehaviors
                .Where(ub => ub.UserId == userId && 
                            (ub.ActionType == "PURCHASE" || ub.ActionType == "ADD_TO_CART" || ub.ActionType == "VIEW"))
                .Include(ub => ub.Product)
                .ThenInclude(p => p.Category)
                .Include(ub => ub.Product)
                .ThenInclude(p => p.Brand)
                .ToListAsync();

            var categories = behaviors
                .Where(b => !string.IsNullOrEmpty(b.Category))
                .GroupBy(b => b.Category)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .Take(5)
                .ToList();

            var brands = behaviors
                .Where(b => !string.IsNullOrEmpty(b.Brand))
                .GroupBy(b => b.Brand)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key!)
                .Take(5)
                .ToList();

            return (categories, brands);
        }

        public async Task<SimilarProductResponse> GetSimilarProductsAsync(SimilarProductRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var cacheKey = $"similar_{request.ProductId}_{request.SimilarityType}_{request.Count}";
                
                if (_cache.TryGetValue(cacheKey, out SimilarProductResponse? cachedResponse))
                {
                    return cachedResponse!;
                }

                var response = new SimilarProductResponse
                {
                    SimilarityType = request.SimilarityType ?? "CONTENT"
                };

                List<SimilarProduct> similarProducts;

                switch (request.SimilarityType?.ToUpperInvariant())
                {
                    case "COLLABORATIVE":
                        similarProducts = await GetCollaborativeSimilarProductsAsync(request);
                        break;
                    case "VISUAL":
                        similarProducts = await GetVisualSimilarProductsAsync(request);
                        break;
                    case "CONTENT":
                    default:
                        similarProducts = await GetContentSimilarProductsAsync(request);
                        break;
                }

                response.Products = similarProducts;
                response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                // Cache for 10 minutes
                _cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products for product {ProductId}", request.ProductId);
                return new SimilarProductResponse
                {
                    SimilarityType = request.SimilarityType ?? "FALLBACK",
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Products = new List<SimilarProduct>()
                };
            }
        }

        private async Task<List<SimilarProduct>> GetContentSimilarProductsAsync(SimilarProductRequest request)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
            {
                return new List<SimilarProduct>();
            }

            var similarProducts = await _context.Products
                .Where(p => p.IsActive && p.Id != request.ProductId)
                .Where(p => p.CategoryId == product.CategoryId || p.BrandId == product.BrandId)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CategoryId == product.CategoryId ? 2 : 1)
                .ThenByDescending(p => p.SalesCount)
                .Take(request.Count)
                .ToListAsync();

            return similarProducts.Select((p, index) => new SimilarProduct
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
                Brand = p.Brand.Name,
                Category = p.Category.Name,
                SimilarityScore = CalculateSimilarityScore(p, product),
                SimilarityReason = GetSimilarityReason(p, product),
                IsInStock = p.InStock
            }).ToList();
        }

        private async Task<List<SimilarProduct>> GetCollaborativeSimilarProductsAsync(SimilarProductRequest request)
        {
            // Get products frequently bought together
            var frequentlyBoughtTogether = await _context.UserBehaviors
                .Where(ub => ub.ActionType == "PURCHASE")
                .GroupBy(ub => ub.UserId)
                .Where(g => g.Any(ub => ub.ProductId == request.ProductId))
                .SelectMany(g => g.Where(ub => ub.ProductId != request.ProductId))
                .GroupBy(ub => ub.ProductId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(request.Count)
                .ToListAsync();

            if (!frequentlyBoughtTogether.Any())
            {
                return await GetContentSimilarProductsAsync(request);
            }

            var products = await _context.Products
                .Where(p => frequentlyBoughtTogether.Contains(p.Id))
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .ToListAsync();

            return products.Select(p => new SimilarProduct
            {
                ProductId = p.Id,
                Name = p.Name,
                Price = p.Price,
                SalePrice = p.SalePrice,
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
                Brand = p.Brand.Name,
                Category = p.Category.Name,
                SimilarityScore = 0.8, // High score for collaborative filtering
                SimilarityReason = "Frequently bought together",
                IsInStock = p.InStock
            }).ToList();
        }

        private async Task<List<SimilarProduct>> GetVisualSimilarProductsAsync(SimilarProductRequest request)
        {
            // For now, fall back to content-based similarity
            // In a real implementation, this would use image analysis
            return await GetContentSimilarProductsAsync(request);
        }

        private double CalculateSimilarityScore(Product product, Product referenceProduct)
        {
            var score = 0.0;

            // Category similarity
            if (product.CategoryId == referenceProduct.CategoryId)
                score += 0.4;

            // Brand similarity
            if (product.BrandId == referenceProduct.BrandId)
                score += 0.3;

            // Price similarity (within 20% range)
            var priceRatio = (double)Math.Min(product.Price, referenceProduct.Price) / (double)Math.Max(product.Price, referenceProduct.Price);
            if (priceRatio > 0.8)
                score += 0.2;

            // Popularity similarity
            var popularityScore = Math.Min(product.SalesCount / 100.0, 1.0);
            score += popularityScore * 0.1;

            return Math.Min(score, 1.0);
        }

        private string GetSimilarityReason(Product product, Product referenceProduct)
        {
            var reasons = new List<string>();

            if (product.CategoryId == referenceProduct.CategoryId)
                reasons.Add("Same category");

            if (product.BrandId == referenceProduct.BrandId)
                reasons.Add("Same brand");

            if (product.IsBestSeller)
                reasons.Add("Best seller");

            return string.Join(", ", reasons);
        }

        public async Task TrackUserBehaviorAsync(UserBehaviorRequest request)
        {
            try
            {
                var behavior = new UserBehavior
                {
                    UserId = request.UserId,
                    ProductId = request.ProductId,
                    ActionType = request.ActionType,
                    SearchQuery = request.SearchQuery,
                    Category = request.Category,
                    Brand = request.Brand,
                    Price = request.Price,
                    Quantity = request.Quantity,
                    SessionId = request.SessionId,
                    DeviceType = request.DeviceType,
                    UserAgent = request.UserAgent
                };

                _context.UserBehaviors.Add(behavior);
                await _context.SaveChangesAsync();

                // Update user profile asynchronously
                _ = Task.Run(async () => await UpdateUserProfileFromBehaviorAsync(request.UserId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user behavior for user {UserId}", request.UserId);
            }
        }

        public async Task RecordRecommendationFeedbackAsync(RecommendationFeedbackRequest request)
        {
            try
            {
                var feedback = new RecommendationFeedback
                {
                    RecommendationId = request.RecommendationId,
                    UserId = request.UserId,
                    FeedbackType = request.FeedbackType,
                    Comment = request.Comment
                };

                _context.RecommendationFeedbacks.Add(feedback);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording recommendation feedback for user {UserId}", request.UserId);
            }
        }

        public async Task<UserProfileResponse> GetUserProfileAsync(string userId)
        {
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return new UserProfileResponse
                {
                    UserId = userId,
                    LastUpdated = DateTime.UtcNow
                };
            }

            return new UserProfileResponse
            {
                UserId = profile.UserId,
                PreferredCategories = JsonSerializer.Deserialize<List<string>>(profile.PreferredCategories ?? "[]") ?? new List<string>(),
                PreferredBrands = JsonSerializer.Deserialize<List<string>>(profile.PreferredBrands ?? "[]") ?? new List<string>(),
                PriceRange = profile.PriceRange,
                ShoppingStyle = profile.ShoppingStyle,
                AverageOrderValue = profile.AverageOrderValue,
                PurchaseFrequency = profile.PurchaseFrequency,
                PreferredDeviceType = profile.PreferredDeviceType,
                LastUpdated = profile.LastUpdated
            };
        }

        public async Task UpdateUserProfileAsync(UserProfileRequest request)
        {
            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (profile == null)
            {
                profile = new UserProfile
                {
                    UserId = request.UserId
                };
                _context.UserProfiles.Add(profile);
            }

            profile.PreferredCategories = JsonSerializer.Serialize(request.PreferredCategories ?? new List<string>());
            profile.PreferredBrands = JsonSerializer.Serialize(request.PreferredBrands ?? new List<string>());
            profile.PriceRange = request.PriceRange;
            profile.ShoppingStyle = request.ShoppingStyle;
            profile.PreferredDeviceType = request.PreferredDeviceType;
            profile.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task UpdateUserProfileFromBehaviorAsync(string userId)
        {
            try
            {
                var behaviors = await _context.UserBehaviors
                    .Where(ub => ub.UserId == userId)
                    .Include(ub => ub.Product)
                    .ThenInclude(p => p.Category)
                    .Include(ub => ub.Product)
                    .ThenInclude(p => p.Brand)
                    .ToListAsync();

                if (!behaviors.Any())
                    return;

                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile == null)
                {
                    profile = new UserProfile { UserId = userId };
                    _context.UserProfiles.Add(profile);
                }

                // Update categories
                var categories = behaviors
                    .Where(b => !string.IsNullOrEmpty(b.Category))
                    .GroupBy(b => b.Category)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key!)
                    .Take(5)
                    .ToList();

                profile.PreferredCategories = JsonSerializer.Serialize(categories);

                // Update brands
                var brands = behaviors
                    .Where(b => !string.IsNullOrEmpty(b.Brand))
                    .GroupBy(b => b.Brand)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key!)
                    .Take(5)
                    .ToList();

                profile.PreferredBrands = JsonSerializer.Serialize(brands);

                // Update average order value
                var purchases = behaviors.Where(b => b.ActionType == "PURCHASE" && b.Price.HasValue).ToList();
                if (purchases.Any())
                {
                    profile.AverageOrderValue = (double)purchases.Average(b => (double)b.Price!.Value);
                }

                // Update purchase frequency
                var purchaseDates = behaviors
                    .Where(b => b.ActionType == "PURCHASE")
                    .Select(b => b.Timestamp.Date)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                if (purchaseDates.Count > 1)
                {
                    var daysBetweenPurchases = purchaseDates
                        .Skip(1)
                        .Zip(purchaseDates, (current, previous) => (current - previous).Days)
                        .Average();
                    profile.PurchaseFrequency = (int)Math.Round(daysBetweenPurchases);
                }

                profile.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for user {UserId}", userId);
            }
        }

        public async Task<List<RecommendationMetrics>> GetRecommendationMetricsAsync(DateTime fromDate, DateTime toDate)
        {
            var metrics = await _context.RecommendationModels
                .Where(rm => rm.GeneratedAt >= fromDate && rm.GeneratedAt <= toDate)
                .GroupBy(rm => rm.AlgorithmType)
                .Select(g => new RecommendationMetrics
                {
                    AlgorithmType = g.Key,
                    TotalRecommendations = g.Count(),
                    ClickedRecommendations = g.Count(rm => _context.RecommendationFeedbacks
                        .Any(rf => rf.RecommendationId == rm.Id && rf.FeedbackType == "CLICKED")),
                    PurchasedRecommendations = g.Count(rm => _context.RecommendationFeedbacks
                        .Any(rf => rf.RecommendationId == rm.Id && rf.FeedbackType == "PURCHASED")),
                    AverageScore = g.Average(rm => rm.Score),
                    CalculatedAt = DateTime.UtcNow
                })
                .ToListAsync();

            foreach (var metric in metrics)
            {
                metric.ClickThroughRate = metric.TotalRecommendations > 0 
                    ? (double)metric.ClickedRecommendations / metric.TotalRecommendations 
                    : 0;
                metric.ConversionRate = metric.TotalRecommendations > 0 
                    ? (double)metric.PurchasedRecommendations / metric.TotalRecommendations 
                    : 0;
            }

            return metrics;
        }

        public Task<ABTestResponse> AssignUserToTestGroupAsync(string userId, string testName)
        {
            // Simple A/B test assignment based on user ID hash
            var hash = userId.GetHashCode();
            var variant = Math.Abs(hash) % 2 == 0 ? "A" : "B";

            return Task.FromResult(new ABTestResponse
            {
                TestId = 1, // In a real implementation, this would come from a test configuration
                TestName = testName,
                AssignedVariant = variant,
                IsActive = true
            });
        }
    }
}
