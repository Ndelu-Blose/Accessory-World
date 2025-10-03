namespace AccessoryWorld.DTOs.AI
{
    public class RecommendationRequest
    {
        public string? UserId { get; set; }
        public int Count { get; set; } = 6;
        public string? AlgorithmType { get; set; } // COLLABORATIVE, CONTENT_BASED, HYBRID, TRENDING
        public string? TestGroup { get; set; }
        public List<string>? ExcludeProductIds { get; set; }
        public List<string>? PreferredCategories { get; set; }
        public List<string>? PreferredBrands { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? MinPrice { get; set; }
        public bool IncludeOutOfStock { get; set; } = false;
    }
    
    public class RecommendationResponse
    {
        public List<RecommendedProduct> Products { get; set; } = new();
        public string AlgorithmUsed { get; set; } = string.Empty;
        public string TestGroup { get; set; } = string.Empty;
        public double ProcessingTimeMs { get; set; }
        public int TotalAvailable { get; set; }
        public string? NextPageToken { get; set; }
    }
    
    public class RecommendedProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double RecommendationScore { get; set; }
        public int Rank { get; set; }
        public string? Reason { get; set; }
        public bool IsInStock { get; set; }
        public bool IsOnSale { get; set; }
        public bool IsNew { get; set; }
        public bool IsBestSeller { get; set; }
        public List<string> Tags { get; set; } = new();
    }
    
    public class SimilarProductRequest
    {
        public int ProductId { get; set; }
        public int Count { get; set; } = 4;
        public string? SimilarityType { get; set; } // CONTENT, COLLABORATIVE, VISUAL
        public List<string>? ExcludeProductIds { get; set; }
    }
    
    public class SimilarProductResponse
    {
        public List<SimilarProduct> Products { get; set; } = new();
        public string SimilarityType { get; set; } = string.Empty;
        public double ProcessingTimeMs { get; set; }
    }
    
    public class SimilarProduct
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string Brand { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string? SimilarityReason { get; set; }
        public bool IsInStock { get; set; }
    }
    
    public class UserBehaviorRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ActionType { get; set; } = string.Empty; // VIEW, ADD_TO_CART, PURCHASE, WISHLIST, SEARCH
        public string? SearchQuery { get; set; }
        public string? Category { get; set; }
        public string? Brand { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? SessionId { get; set; }
        public string? DeviceType { get; set; }
        public string? UserAgent { get; set; }
    }
    
    public class RecommendationFeedbackRequest
    {
        public int RecommendationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FeedbackType { get; set; } = string.Empty; // CLICKED, PURCHASED, DISMISSED, LIKED, DISLIKED
        public string? Comment { get; set; }
    }
    
    public class UserProfileRequest
    {
        public string UserId { get; set; } = string.Empty;
        public List<string>? PreferredCategories { get; set; }
        public List<string>? PreferredBrands { get; set; }
        public string? PriceRange { get; set; } // LOW, MEDIUM, HIGH, LUXURY
        public string? ShoppingStyle { get; set; } // BUDGET, PREMIUM, TRENDY, CLASSIC
        public string? PreferredDeviceType { get; set; } // MOBILE, DESKTOP, TABLET
    }
    
    public class UserProfileResponse
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> PreferredCategories { get; set; } = new();
        public List<string> PreferredBrands { get; set; } = new();
        public string? PriceRange { get; set; }
        public string? ShoppingStyle { get; set; }
        public double? AverageOrderValue { get; set; }
        public int? PurchaseFrequency { get; set; }
        public string? PreferredDeviceType { get; set; }
        public DateTime LastUpdated { get; set; }
    }
    
    public class RecommendationMetrics
    {
        public string AlgorithmType { get; set; } = string.Empty;
        public int TotalRecommendations { get; set; }
        public int ClickedRecommendations { get; set; }
        public int PurchasedRecommendations { get; set; }
        public double ClickThroughRate { get; set; }
        public double ConversionRate { get; set; }
        public double AverageScore { get; set; }
        public DateTime CalculatedAt { get; set; }
    }
    
    public class ABTestRequest
    {
        public string TestName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Variants { get; set; } = new();
        public double TrafficAllocation { get; set; } = 1.0; // 0.0 to 1.0
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> TargetUserSegments { get; set; } = new();
    }
    
    public class ABTestResponse
    {
        public int TestId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string AssignedVariant { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
