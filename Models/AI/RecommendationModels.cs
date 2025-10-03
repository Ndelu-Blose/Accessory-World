using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models.AI
{
    public class UserBehavior
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // VIEW, ADD_TO_CART, PURCHASE, WISHLIST, SEARCH
        
        [MaxLength(100)]
        public string? SearchQuery { get; set; }
        
        [MaxLength(50)]
        public string? Category { get; set; }
        
        [MaxLength(50)]
        public string? Brand { get; set; }
        
        public decimal? Price { get; set; }
        
        public int? Quantity { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? SessionId { get; set; }
        
        [MaxLength(50)]
        public string? DeviceType { get; set; } // MOBILE, DESKTOP, TABLET
        
        [MaxLength(100)]
        public string? UserAgent { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
    
    public class RecommendationModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string AlgorithmType { get; set; } = string.Empty; // COLLABORATIVE, CONTENT_BASED, HYBRID, TRENDING
        
        [Required]
        public double Score { get; set; } // 0.0 to 1.0
        
        [Required]
        public int Rank { get; set; } // Position in recommendation list
        
        [MaxLength(500)]
        public string? Reason { get; set; } // Why this product was recommended
        
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // A/B Testing
        [MaxLength(50)]
        public string? TestGroup { get; set; }
        
        [MaxLength(50)]
        public string? TestVariant { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
    
    public class RecommendationFeedback
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int RecommendationId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string FeedbackType { get; set; } = string.Empty; // CLICKED, PURCHASED, DISMISSED, LIKED, DISLIKED
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(500)]
        public string? Comment { get; set; }
        
        // Navigation properties
        public virtual RecommendationModel Recommendation { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
    
    public class ProductSimilarity
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ProductId1 { get; set; }
        
        [Required]
        public int ProductId2 { get; set; }
        
        [Required]
        public double SimilarityScore { get; set; } // 0.0 to 1.0
        
        [Required]
        [MaxLength(50)]
        public string SimilarityType { get; set; } = string.Empty; // CONTENT, COLLABORATIVE, VISUAL
        
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        // Navigation properties
        public virtual Product Product1 { get; set; } = null!;
        public virtual Product Product2 { get; set; } = null!;
    }
    
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? PreferredCategories { get; set; } // JSON array
        
        [MaxLength(500)]
        public string? PreferredBrands { get; set; } // JSON array
        
        [MaxLength(100)]
        public string? PriceRange { get; set; } // LOW, MEDIUM, HIGH, LUXURY
        
        [MaxLength(50)]
        public string? ShoppingStyle { get; set; } // BUDGET, PREMIUM, TRENDY, CLASSIC
        
        public double? AverageOrderValue { get; set; }
        
        public int? PurchaseFrequency { get; set; } // Days between purchases
        
        [MaxLength(50)]
        public string? PreferredDeviceType { get; set; } // MOBILE, DESKTOP, TABLET
        
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
