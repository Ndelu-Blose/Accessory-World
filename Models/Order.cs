using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string OrderNumber { get; set; } = string.Empty;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int ShippingAddressId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, PAID, PROCESSING, SHIPPED, DELIVERED, CANCELLED, REFUNDED
        
        [Required]
        [MaxLength(20)]
        public string FulfilmentMethod { get; set; } = string.Empty; // DELIVERY, PICKUP
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal SubTotal { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingFee { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal CreditNoteAmount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }
        
        [MaxLength(10)]
        public string Currency { get; set; } = "ZAR";
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Address ShippingAddress { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual Shipment? Shipment { get; set; }
        public virtual PickupOTP? PickupOTP { get; set; }
        public virtual ICollection<RMA> RMAs { get; set; } = new List<RMA>();
    }
    
    public class OrderItem
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int SKUId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; } // Price at time of order (immutable snapshot)
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal LineTotal { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, FULFILLED, CANCELLED, REFUNDED
        
        public int RefundedQuantity { get; set; } = 0;
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual SKU SKU { get; set; } = null!;
    }
    
    public class Payment
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string PaymentIntentId { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TransactionId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Method { get; set; } = string.Empty; // CARD, EFT, etc.
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCEEDED, FAILED, CANCELLED, REFUNDED
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal RefundedAmount { get; set; } = 0;
        
        [MaxLength(10)]
        public string Currency { get; set; } = "ZAR";
        
        [MaxLength(500)]
        public string? FailureReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
    }
    
    public class Shipment
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string CourierCode { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }
        
        [MaxLength(500)]
        public string? LabelUrl { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "PREPARING"; // PREPARING, READY_FOR_DISPATCH, IN_TRANSIT, DELIVERED, EXCEPTION
        
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        
        [MaxLength(500)]
        public string? ProofOfDelivery { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
    }
    
    public class PickupOTP
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string OTPCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, USED, EXPIRED
        
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        
        [MaxLength(100)]
        public string? UsedByStaffId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
    }
    
    public class RMA
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string RMANumber { get; set; } = string.Empty;
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "REQUESTED"; // REQUESTED, APPROVED, RECEIVED, PROCESSED, REFUNDED
        
        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(10,2)")]
        public decimal RefundAmount { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        
        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<RMAItem> RMAItems { get; set; } = new List<RMAItem>();
    }
    
    public class RMAItem
    {
        public int Id { get; set; }
        
        [Required]
        public int RMAId { get; set; }
        
        [Required]
        public int OrderItemId { get; set; }
        
        [Required]
        public int Quantity { get; set; }
        
        [MaxLength(20)]
        public string Condition { get; set; } = "UNKNOWN"; // SALEABLE, DAMAGED, DEFECTIVE
        
        // Navigation properties
        public virtual RMA RMA { get; set; } = null!;
        public virtual OrderItem OrderItem { get; set; } = null!;
    }
}