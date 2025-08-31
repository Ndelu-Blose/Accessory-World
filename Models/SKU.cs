using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class SKU
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string SKUCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? Variant { get; set; } // e.g., "Black", "64GB", etc.
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? CompareAtPrice { get; set; }
        
        public int StockQuantity { get; set; } = 0;
        public int ReservedQuantity { get; set; } = 0;
        public int LowStockThreshold { get; set; } = 5;
        
        [MaxLength(50)]
        public string? Barcode { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Calculated property
        public int AvailableQuantity => StockQuantity - ReservedQuantity;
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
}