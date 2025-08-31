using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        
        [Required]
        public int SKUId { get; set; }
        
        [Required]
        public int Quantity { get; set; } // Positive for inbound, negative for outbound
        
        [Required]
        [MaxLength(50)]
        public string MovementType { get; set; } = string.Empty; // SALE, RECEIPT, ADJUSTMENT, RETURN, etc.
        
        [Required]
        [MaxLength(100)]
        public string ReasonCode { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Order ID, RMA ID, etc.
        
        [Required]
        public string UserId { get; set; } = string.Empty; // Who made the movement
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual SKU SKU { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}