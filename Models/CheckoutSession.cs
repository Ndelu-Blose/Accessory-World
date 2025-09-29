using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class CheckoutSession
    {
        [Key]
        public Guid SessionId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, COMPLETED, EXPIRED, CANCELLED
        
        public string? AppliedCreditNoteCode { get; set; }
        
        public decimal CreditNoteAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<StockLock> StockLocks { get; set; } = new List<StockLock>();
        public virtual ICollection<CreditNoteLock> CreditNoteLocks { get; set; } = new List<CreditNoteLock>();
    }
}