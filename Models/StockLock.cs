using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class StockLock
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid SessionId { get; set; }
        
        [Required]
        public int SKUId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "LOCKED"; // LOCKED, RELEASED, CONSUMED
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime? ReleasedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("SessionId")]
        public virtual CheckoutSession CheckoutSession { get; set; } = null!;
        
        [ForeignKey("SKUId")]
        public virtual SKU SKU { get; set; } = null!;
    }
}