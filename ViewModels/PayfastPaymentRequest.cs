using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class PayfastPaymentRequest
    {
        [Required(ErrorMessage = "Merchant ID is required")]
        [StringLength(10, ErrorMessage = "Merchant ID cannot exceed 10 characters")]
        public string MerchantId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Merchant Key is required")]
        [StringLength(32, ErrorMessage = "Merchant Key cannot exceed 32 characters")]
        public string MerchantKey { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Return URL is required")]
        [Url(ErrorMessage = "Return URL must be a valid URL")]
        public string ReturnUrl { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Cancel URL is required")]
        [Url(ErrorMessage = "Cancel URL must be a valid URL")]
        public string CancelUrl { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Notify URL is required")]
        [Url(ErrorMessage = "Notify URL must be a valid URL")]
        public string NotifyUrl { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters and spaces")]
        public string NameFirst { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Last name can only contain letters and spaces")]
        public string NameLast { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
        public string EmailAddress { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Payment ID is required")]
        [StringLength(100, ErrorMessage = "Payment ID cannot exceed 100 characters")]
        public string MPaymentId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Amount is required")]
        [RegularExpression(@"^\d+\.\d{2}$", ErrorMessage = "Amount must be in format 0.00")]
        public string Amount { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string ItemName { get; set; } = string.Empty;
        
        [StringLength(255, ErrorMessage = "Item description cannot exceed 255 characters")]
        public string ItemDescription { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Signature is required")]
        [StringLength(32, ErrorMessage = "Signature must be exactly 32 characters")]
        public string Signature { get; set; } = string.Empty;
    }
}