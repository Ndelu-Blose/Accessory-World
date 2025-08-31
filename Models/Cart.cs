using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        [Required]
        public string SessionId { get; set; } = string.Empty;
        
        public string? UserId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int SKUId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual SKU SKU { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }
        
        // Calculated properties
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;
    }
    
    public class Cart
    {
        public string SessionId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        
        // Calculated properties
        [NotMapped]
        public decimal SubTotal => Items.Sum(item => item.TotalPrice);
        
        [NotMapped]
        public int TotalItems => Items.Sum(item => item.Quantity);
        
        [NotMapped]
        public decimal VATAmount => SubTotal * 0.15m; // 15% VAT for South Africa
        
        [NotMapped]
        public decimal Total => SubTotal + VATAmount;
    }
}