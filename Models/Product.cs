using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AccessoryWorld.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public int BrandId { get; set; }
        
        [Required]
        public int CategoryId { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        public bool IsOnSale { get; set; } = false;
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? SalePrice { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? CompareAtPrice { get; set; } // for strikethrough pricing
        
        [MaxLength(50)]
        public string? Model { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsBestSeller { get; set; } = false;
        public bool IsNew { get; set; } = false;
        public bool IsHot { get; set; } = false;
        public bool IsTodayDeal { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public bool InStock { get; set; } = true;
        public int SalesCount { get; set; } = 0;
        public int ViewCount { get; set; } = 0;
        
        [MaxLength(20)]
        public string Condition { get; set; } = "New"; // New or C.P.O
        
        public string Tags { get; set; } = string.Empty; // JSON array as string
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Computed properties
        public int StockQuantity => SKUs?.Sum(s => s.StockQuantity) ?? 0;
        public int LowStockThreshold => SKUs?.FirstOrDefault()?.LowStockThreshold ?? 5;
        
        // Navigation properties
        public virtual Brand Brand { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<SKU> SKUs { get; set; } = new List<SKU>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductSpecification> ProductSpecifications { get; set; } = new List<ProductSpecification>();
    }
    

}