using AccessoryWorld.Models;
using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Cart items are required")]
        [MinLength(1, ErrorMessage = "Cart must contain at least one item")]
        public List<CartItem> CartItems { get; set; } = [];
        
        public List<Address> UserAddresses { get; set; } = [];
        
        [Required(ErrorMessage = "Fulfillment method is required")]
        [RegularExpression("^(delivery|pickup)$", ErrorMessage = "Fulfillment method must be either 'delivery' or 'pickup'")]
        public string FulfillmentMethod { get; set; } = "delivery";
        
        public int? SelectedAddressId { get; set; }
        
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }
        
        // Calculated values
        [Range(0, double.MaxValue, ErrorMessage = "Subtotal must be non-negative")]
        public decimal SubTotal { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "VAT amount must be non-negative")]
        public decimal VATAmount { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Shipping fee must be non-negative")]
        public decimal ShippingFee { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Total must be non-negative")]
        public decimal Total { get; set; }
        
        // Store pickup information
        public string StoreAddress { get; set; } = "123 Main Street, Cape Town, 8001";
        public string StoreHours { get; set; } = "Monday - Friday: 9:00 AM - 6:00 PM, Saturday: 9:00 AM - 4:00 PM";
        
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string StorePhone { get; set; } = "+27 21 123 4567";
    }
}