using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class WebhookEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string EventId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Source { get; set; } = string.Empty; // PAYFAST, STRIPE, TRADEIN_SYSTEM, etc.

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // PENDING, PROCESSED, FAILED, DUPLICATE

        [Column(TypeName = "nvarchar(max)")]
        public string? Payload { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? ProcessingResult { get; set; }

        public string? ErrorMessage { get; set; }

        public int? RelatedOrderId { get; set; }
        public int? RelatedTradeInCaseId { get; set; }
        public int? RelatedCreditNoteId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        public int RetryCount { get; set; } = 0;
        public DateTime? NextRetryAt { get; set; }

        // Navigation properties
        [ForeignKey("RelatedOrderId")]
        public Order? RelatedOrder { get; set; }

        [ForeignKey("RelatedTradeInCaseId")]
        public TradeInCase? RelatedTradeInCase { get; set; }

        [ForeignKey("RelatedCreditNoteId")]
        public CreditNote? RelatedCreditNote { get; set; }
    }
}