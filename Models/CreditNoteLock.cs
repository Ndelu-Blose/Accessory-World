using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class CreditNoteLock
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public Guid SessionId { get; set; }
        
        [Required]
        public string CreditNoteCode { get; set; } = string.Empty;
        
        [Required]
        public decimal LockedAmount { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "LOCKED"; // LOCKED, RELEASED, CONSUMED
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime? ReleasedAt { get; set; }
        
        // Navigation properties
        [ForeignKey("SessionId")]
        public virtual CheckoutSession CheckoutSession { get; set; } = null!;
        
        [ForeignKey("CreditNoteCode")]
        public virtual CreditNote CreditNote { get; set; } = null!;
    }
}