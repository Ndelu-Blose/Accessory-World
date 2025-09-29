using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class TradeIn
    {
        [Key]
        public int Id { get; set; }
        
        public Guid PublicId { get; set; } = Guid.NewGuid();
        
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(64)]
        public string DeviceBrand { get; set; } = "Apple";
        
        [Required]
        [MaxLength(128)]
        public string DeviceModel { get; set; } = string.Empty;
        
        [MaxLength(64)]
        public string? DeviceType { get; set; }
        
        [MaxLength(32)]
        public string? IMEI { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(2)]
        public string ConditionGrade { get; set; } = string.Empty; // A, B, C, D
        
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string PhotosJson { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProposedValue { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ApprovedValue { get; set; }
        
        [Column(TypeName = "nvarchar(max)")]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReviewedAt { get; set; }
        
        public string? ApprovedBy { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[0];
        
        // Navigation properties
        public virtual ApplicationUser Customer { get; set; } = null!;
        public virtual ApplicationUser? ApprovedByUser { get; set; }
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public virtual CreditNote? CreditNote { get; set; }
        public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    }
    
    public class CreditNote
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public int? TradeInCaseId { get; set; }
        
        public int? ConsumedInOrderId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string CreditNoteCode { get; set; } = string.Empty;
        
        [Required]
        public int TradeInId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountRemaining { get; set; }
        
        [Required]
        [MaxLength(32)]
        public string Status { get; set; } = string.Empty;
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? RedeemedAt { get; set; }
        
        public int? RedeemedOrderId { get; set; }
        
        [Timestamp]
        public byte[] RowVersion { get; set; } = new byte[0];
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual TradeInCase? TradeInCase { get; set; }
        public virtual Order? ConsumedInOrder { get; set; }
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    }
    
    public class StockItem
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int SKUId { get; set; }
        
        [Required]
        public bool IsTradeInUnit { get; set; } = false;
        
        public int? SourceTradeInId { get; set; }
        
        // Other existing properties would be here
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual SKU SKU { get; set; } = null!;
        public virtual TradeIn? SourceTradeIn { get; set; }
    }
    
    // Keep existing classes for backward compatibility
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
        public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
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
}