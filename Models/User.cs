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
        
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes.")]
        public string FullName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [RegularExpression(@"^(\+27|0)[0-9]{9}$", ErrorMessage = "Please enter a valid South African phone number.")]
        public string PhoneNumber { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Address line 1 is required.")]
        [MaxLength(200)]
        [MinLength(5, ErrorMessage = "Address line 1 must be at least 5 characters.")]
        public string AddressLine1 { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }
        
        [Required(ErrorMessage = "City is required.")]
        [MaxLength(100)]
        [MinLength(2, ErrorMessage = "City must be at least 2 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'-]+$", ErrorMessage = "City can only contain letters, spaces, hyphens, and apostrophes.")]
        public string City { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Province is required.")]
        [MaxLength(100)]
        public string Province { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Postal code is required.")]
        [MaxLength(10)]
        [MinLength(4, ErrorMessage = "Postal code must be at least 4 characters.")]
        [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Postal code must be exactly 4 digits for South Africa.")]
        public string PostalCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Country is required.")]
        [MaxLength(100)]
        public string Country { get; set; } = "South Africa";
        
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}