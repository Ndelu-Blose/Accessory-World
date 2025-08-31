using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class TradeInCase
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string CaseNumber { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string DeviceBrand { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string DeviceModel { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string IMEI { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? SerialNumber { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string EvaluationMethod { get; set; } = string.Empty; // IN_STORE, COURIER
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "SUBMITTED"; // SUBMITTED, AWAITING_EVALUATION, EVALUATED, OFFER_SENT, ACCEPTED, REJECTED, EXPIRED, COMPLETED
        
        [MaxLength(1000)]
        public string? CustomerNotes { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal? OfferAmount { get; set; }
        
        public DateTime? OfferExpiresAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<TradeInImage> Images { get; set; } = new List<TradeInImage>();
        public virtual TradeInEvaluation? Evaluation { get; set; }
        public virtual CreditNote? CreditNote { get; set; }
    }
    
    public class TradeInImage
    {
        public int Id { get; set; }
        
        [Required]
        public int TradeInCaseId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? ImageType { get; set; } // FRONT, BACK, SCREEN, DAMAGE, etc.
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual TradeInCase TradeInCase { get; set; } = null!;
    }
    
    public class TradeInEvaluation
    {
        public int Id { get; set; }
        
        [Required]
        public int TradeInCaseId { get; set; }
        
        [Required]
        public string EvaluatedByUserId { get; set; } = string.Empty;
        
        // Device condition scores (1-5 scale)
        public int ScreenCondition { get; set; } = 0;
        public int BodyCondition { get; set; } = 0;
        public int BatteryHealth { get; set; } = 0;
        public int FunctionalityScore { get; set; } = 0;
        
        public bool HasOriginalBox { get; set; } = false;
        public bool HasCharger { get; set; } = false;
        public bool HasEarphones { get; set; } = false;
        public bool IsUnlocked { get; set; } = false;
        public bool IsBlacklisted { get; set; } = false;
        
        [MaxLength(1000)]
        public string? EvaluatorNotes { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal BaseValue { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal ConditionAdjustment { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal AccessoryBonus { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalOfferAmount { get; set; }
        
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual TradeInCase TradeInCase { get; set; } = null!;
        public virtual ApplicationUser EvaluatedByUser { get; set; } = null!;
    }
    
    public class CreditNote
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string CreditNoteCode { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public int? TradeInCaseId { get; set; } // Nullable for admin-issued credits
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal RemainingAmount { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, CONSUMED, EXPIRED, CANCELLED
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public int? ConsumedInOrderId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual TradeInCase? TradeInCase { get; set; }
        public virtual Order? ConsumedInOrder { get; set; }
    }
    
    public class DeviceModel
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Variant { get; set; } // Storage, Color, etc.
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal BaseTradeInValue { get; set; }
        
        public bool IsEligibleForTradeIn { get; set; } = true;
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}