using AccessoryWorld.Models;
using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class PaymentViewModel
    {
        [Required(ErrorMessage = "Order information is required")]
        public Order Order { get; set; } = new Order();
        
        [Required(ErrorMessage = "Order items are required")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item")]
        public List<OrderItem> OrderItems { get; set; } = [];
        
        public Address? DeliveryAddress { get; set; }
        
        [Required(ErrorMessage = "Payment method is required")]
        [RegularExpression("^(card|eft)$", ErrorMessage = "Payment method must be either 'card' or 'eft'")]
        public string PaymentMethod { get; set; } = "card";
        
        // Card payment fields
        [CreditCard(ErrorMessage = "Invalid credit card number")]
        [StringLength(19, MinimumLength = 13, ErrorMessage = "Card number must be between 13 and 19 digits")]
        public string? CardNumber { get; set; }
        
        [StringLength(100, ErrorMessage = "Card holder name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Card holder name can only contain letters and spaces")]
        public string? CardHolderName { get; set; }
        
        [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12")]
        public string? ExpiryMonth { get; set; }
        
        [Range(2024, 2034, ErrorMessage = "Expiry year must be between 2024 and 2034")]
        public string? ExpiryYear { get; set; }
        
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3 or 4 digits")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must contain only digits")]
        public string? CVV { get; set; }
        
        // EFT payment fields
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        public string? BankName { get; set; }
        
        [StringLength(100, ErrorMessage = "Account holder name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Account holder name can only contain letters and spaces")]
        public string? AccountHolder { get; set; }
        
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Account number must be between 8 and 20 digits")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Account number must contain only digits")]
        public string? AccountNumber { get; set; }
        
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Branch code must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Branch code must be exactly 6 digits")]
        public string? BranchCode { get; set; }
        
        // Store pickup information (if applicable)
        public string? StoreAddress { get; set; }
        
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Pickup OTP must be exactly 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pickup OTP must be exactly 6 digits")]
        public string? PickupOTP { get; set; }
    }
}