using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class OrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKUName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public string Variant { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class OrderCheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();
        
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public decimal Total { get; set; }
        
        public List<AddressViewModel> UserAddresses { get; set; } = new List<AddressViewModel>();
        
        // Checkout form properties
        public Guid? SelectedAddressId { get; set; }
        public Guid? ShippingAddressId { get; set; }
        public string DeliveryMethod { get; set; } = "Standard";
        public string FulfillmentMethod { get; set; } = "Delivery";
        public string PaymentMethod { get; set; } = "Card";
        public string? SpecialInstructions { get; set; }
        public string? Notes { get; set; }
        public decimal VATAmount { get; set; }
        
        // Credit Note properties
        public string? CreditNoteCode { get; set; }
        public decimal? CreditNoteRequestedAmount { get; set; }
        public bool HasValidCreditNote { get; set; }
        public decimal AvailableCreditBalance { get; set; }
        public List<CreditNoteViewModel> UserCreditNotes { get; set; } = new List<CreditNoteViewModel>();
        
        // Inline address form for workaround (when no addresses exist or user wants to add new)
        public bool UseInlineAddress { get; set; } = false;
        public AddressViewModel InlineAddress { get; set; } = new AddressViewModel();
    }

    public class CartItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
        public string? ImageUrl { get; set; }
    }

    // Alias for compatibility
    public class CheckoutViewModel : OrderCheckoutViewModel
    {
    }
}