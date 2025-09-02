using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        
        // Trade-in statistics
        public int PendingTradeIns { get; set; }
        public int TodayTradeIns { get; set; }
        public int WeeklyTradeIns { get; set; }
        public int MonthlyTradeIns { get; set; }
        
        // Credit note statistics
        public int ActiveCreditNotes { get; set; }
        public int TotalCreditNotesIssued { get; set; }
        public decimal TotalCreditValue { get; set; }
        
        // Enhanced user statistics
        public int NewCustomersThisMonth { get; set; }
        
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new List<RecentOrderViewModel>();
        public List<LowStockProductViewModel> LowStockProducts { get; set; } = new List<LowStockProductViewModel>();
        public List<MonthlySalesViewModel> MonthlySales { get; set; } = new List<MonthlySalesViewModel>();
    }

    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class LowStockProductViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
    }

    public class MonthlySalesViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalSales { get; set; }
        public int OrderCount { get; set; }
        
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class AdminOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FulfilmentMethod { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public int ItemCount { get; set; }
        public string? TrackingNumber { get; set; }
    }

    public class AdminProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SKUCount { get; set; }
        public int TotalStock { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 5;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class AdminInventoryViewModel
    {
        public int SKUId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public string? Variant { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int CurrentStock { get; set; }
        public int ReservedStock { get; set; }
        public int AvailableStock { get; set; }
        public int LowStockThreshold { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Computed properties for display
        public int AvailableQuantity => CurrentStock - ReservedStock;
    }

    public class SalesReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalItemsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();
        public List<DailySalesViewModel> DailySales { get; set; } = new List<DailySalesViewModel>();
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DailySalesViewModel
    {
        public DateTime Date { get; set; }
        public decimal Sales { get; set; }
        public int Orders { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AddInventoryItemViewModel
    {
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string SKUCode { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string? Variant { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Compare at price must be greater than 0")]
        public decimal? CompareAtPrice { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        public int StockQuantity { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold must be 0 or greater")]
        public int LowStockThreshold { get; set; } = 5;
    }

    public class SettingsViewModel
    {
        // General Settings
        public string SiteName { get; set; } = "AccessoryWorld";
        public string SiteEmail { get; set; } = "admin@accessoryworld.com";
        public string Currency { get; set; } = "USD";
        public bool MaintenanceMode { get; set; } = false;
        
        // Email Settings
        public string? SmtpHost { get; set; }
        public int? SmtpPort { get; set; }
        public string? SmtpUsername { get; set; }
        public string? SmtpPassword { get; set; }
        public bool SmtpEnableSsl { get; set; } = true;
        
        // Security Settings
        public bool RequireEmailConfirmation { get; set; } = true;
        public bool EnableTwoFactorAuth { get; set; } = false;
        public int SessionTimeoutMinutes { get; set; } = 30;
        public int MaxLoginAttempts { get; set; } = 5;
        
        // Payment Settings
        public string? PaymentGateway { get; set; } = "Stripe";
        public string? PaymentApiKey { get; set; }
        public string? PaymentSecretKey { get; set; }
        public bool EnablePayPal { get; set; } = false;
        public bool EnableCreditCard { get; set; } = true;
        public decimal TaxRate { get; set; } = 8.5m;
        public decimal ShippingCost { get; set; } = 9.99m;
    }

    public class AddProductViewModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Compare at price must be greater than 0")]
        public decimal? CompareAtPrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold must be 0 or greater")]
        public int LowStockThreshold { get; set; } = 5;

        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(50, ErrorMessage = "SKU code cannot exceed 50 characters")]
        public string? SKUCode { get; set; }
    }

    public class EditProductViewModel
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Brand is required")]
        public int BrandId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Compare at price must be greater than 0")]
        public decimal? CompareAtPrice { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Sale price must be greater than 0")]
        public decimal? SalePrice { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be 0 or greater")]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold must be 0 or greater")]
        public int LowStockThreshold { get; set; } = 5;

        [Url(ErrorMessage = "Please enter a valid URL")]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsNew { get; set; }
        public bool IsHot { get; set; }
        public bool IsTodayDeal { get; set; }
        public bool IsBestSeller { get; set; }
        public bool IsOnSale { get; set; }

        [StringLength(50, ErrorMessage = "SKU code cannot exceed 50 characters")]
        public string? SKUCode { get; set; }

        // Display properties
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int SalesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Image management
        public List<ProductImageViewModel> CurrentImages { get; set; } = new List<ProductImageViewModel>();
        public List<int> ImagesToDelete { get; set; } = new List<int>();
        public int? PrimaryImageId { get; set; }
    }

    public class ProductImageViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class StockWriteOffViewModel
    {
        public int Id { get; set; }
        public string SKUCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? ReferenceNumber { get; set; }
        public string? ImageUrl { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class ShippingLabelViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public string Carrier { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal ShippingCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = "Generated";
        public string? LabelUrl { get; set; }
        public string CourierCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal OrderTotal { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime? GeneratedAt { get; set; }
    }

    public class ProductPromotionsViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string PromotionType { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public int UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public int TotalPromotions { get; set; }
        public int ActivePromotions { get; set; }
        public int ExpiringPromotions { get; set; }
        public decimal TotalSavings { get; set; }
    }

    public class PurchaseOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = "Pending";
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseOrderItemViewModel> Items { get; set; } = new List<PurchaseOrderItemViewModel>();
        public string PONumber { get; set; } = string.Empty;
        public string SupplierEmail { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public int ItemCount { get; set; }
        public string Priority { get; set; } = "Medium";
    }

    public class PurchaseOrderItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class ProductBrandsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalBrands { get; set; }
        public int FeaturedBrands { get; set; }
        public int EmptyBrands { get; set; }
        public int ActiveBrands { get; set; }
    }

    public class ProductCategoriesViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalCategories { get; set; }
        public int ActiveCategories { get; set; }
        public int ParentCategories { get; set; }
        public int EmptyCategories { get; set; }
    }

    public class PaymentConfirmationViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal RefundedAmount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
    }

    public class PickupOTPViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string OTPCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime? GeneratedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public string Status { get; set; } = "Active";
        public decimal OrderTotal { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
        public int OrderId { get; set; }
        public bool IsExpiringSoon { get; set; }
        public TimeSpan? TimeUntilExpiry { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string? VerifiedBy { get; set; }
    }

    public class StockMovementViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime MovementDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProductImageUrl { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? UserName { get; set; }
    }

    public class ManageInventoryViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int LowStockThreshold { get; set; }
        public decimal UnitCost { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public string? Location { get; set; }
        public bool IsLowStock { get; set; }
        public int ReservedStock { get; set; }
        public int AvailableStock { get; set; }
    }

    public class FeaturedProductsViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKUCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public int SalesCount { get; set; }
        public DateTime FeaturedDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public int TotalFeatured { get; set; }
        public int ActiveFeatured { get; set; }
        public int BestSellers { get; set; }
        public int TodayDeals { get; set; }
    }
}