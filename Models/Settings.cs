using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class Settings
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SiteName { get; set; } = "AccessoryWorld";
        
        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string SiteEmail { get; set; } = "admin@accessoryworld.com";
        
        [Required]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";
        
        public bool MaintenanceMode { get; set; } = false;
        
        // Email Settings
        [StringLength(100)]
        public string? SmtpHost { get; set; }
        
        public int? SmtpPort { get; set; }
        
        [StringLength(100)]
        public string? SmtpUsername { get; set; }
        
        [StringLength(100)]
        public string? SmtpPassword { get; set; }
        
        public bool SmtpEnableSsl { get; set; } = true;
        
        // Security Settings
        public bool RequireEmailConfirmation { get; set; } = true;
        
        public bool EnableTwoFactorAuth { get; set; } = false;
        
        public int SessionTimeoutMinutes { get; set; } = 30;
        
        public int MaxLoginAttempts { get; set; } = 5;
        
        // Payment Settings
        [StringLength(100)]
        public string? PaymentGateway { get; set; } = "Stripe";
        
        [StringLength(100)]
        public string? PaymentApiKey { get; set; }
        
        [StringLength(100)]
        public string? PaymentSecretKey { get; set; }
        
        public bool EnablePayPal { get; set; } = false;
        
        public bool EnableCreditCard { get; set; } = true;
        
        public decimal TaxRate { get; set; } = 8.5m;
        
        public decimal ShippingCost { get; set; } = 9.99m;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}