using System.ComponentModel.DataAnnotations;
using AccessoryWorld.Models;

namespace AccessoryWorld.ViewModels
{
    public class ProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "First name cannot be longer than 100 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100, ErrorMessage = "Last name cannot be longer than 100 characters.")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class AddressViewModel
    {
        public int Id { get; set; }
        
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address line 1 is required.")]
        [Display(Name = "Address Line 1")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Address line 1 must be between 5 and 200 characters.")]
        public string AddressLine1 { get; set; } = string.Empty;

        [Display(Name = "Address Line 2")]
        [StringLength(200, ErrorMessage = "Address line 2 cannot be longer than 200 characters.")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [Display(Name = "City")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "City can only contain letters, spaces, hyphens, and apostrophes.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required.")]
        [Display(Name = "Province")]
        [StringLength(100, ErrorMessage = "Province cannot be longer than 100 characters.")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required.")]
        [Display(Name = "Postal Code")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Postal code must be between 4 and 10 characters.")]
        [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Postal code must be exactly 4 digits for South Africa.")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [Display(Name = "Country")]
        [StringLength(100, ErrorMessage = "Country cannot be longer than 100 characters.")]
        public string Country { get; set; } = "South Africa";

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^(\+27|0)[0-9]{9}$", ErrorMessage = "Please enter a valid South African phone number (e.g., +27123456789 or 0123456789).")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Set as Default Address")]
        public bool IsDefault { get; set; } = false;
    }

    public class CustomerDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int SavedAddresses { get; set; }
        public int WishlistItems { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime MemberSince { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<RecentOrderSummary> RecentOrders { get; set; } = new List<RecentOrderSummary>();
        public List<DashboardNotification> Notifications { get; set; } = new List<DashboardNotification>();
        public List<Product> RecommendedProducts { get; set; } = new List<Product>();
        
        public string FullName => $"{FirstName} {LastName}";
        public string MembershipDuration
        {
            get
            {
                var duration = DateTime.Now - MemberSince;
                if (duration.TotalDays < 30)
                    return $"{(int)duration.TotalDays} days";
                else if (duration.TotalDays < 365)
                    return $"{(int)(duration.TotalDays / 30)} months";
                else
                    return $"{(int)(duration.TotalDays / 365)} years";
            }
        }
    }

    public class RecentOrderSummary
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public string? TrackingNumber { get; set; }
        public bool CanReorder { get; set; }
        public bool CanTrack { get; set; }
        public string StatusColor
        {
            get
            {
                return Status.ToLower() switch
                {
                    "pending" => "warning",
                    "processing" => "info",
                    "shipped" => "primary",
                    "delivered" => "success",
                    "cancelled" => "danger",
                    _ => "secondary"
                };
            }
        }
    }

    public class DashboardNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // info, success, warning, danger
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionText { get; set; }
        public string Icon
        {
            get
            {
                return Type switch
                {
                    "success" => "fas fa-check-circle",
                    "warning" => "fas fa-exclamation-triangle",
                    "danger" => "fas fa-times-circle",
                    _ => "fas fa-info-circle"
                };
            }
        }
    }

public class OrderHistoryViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Currency { get; set; } = "ZAR";
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string FulfilmentMethod { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
}

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal CreditNoteAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string FulfilmentMethod { get; set; } = string.Empty;
    public AddressViewModel ShippingAddress { get; set; } = new AddressViewModel();
    public ShipmentViewModel? Shipment { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
    public List<PaymentHistoryViewModel> Payments { get; set; } = new List<PaymentHistoryViewModel>();
    public bool HasActiveRMA { get; set; }
}



public class ShipmentViewModel
{
    public string CourierCode { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
}

public class PaymentHistoryViewModel
{
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
}