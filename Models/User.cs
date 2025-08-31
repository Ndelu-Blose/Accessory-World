using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<TradeInCase> TradeInCases { get; set; } = new List<TradeInCase>();
        public virtual ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();
        public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
    
    public class Address
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(10)]
        public string PostalCode { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = "South Africa";
        
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}