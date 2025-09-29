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
        public int Id { get; set; }                        // internal PK (keep as int)
        [Required] public Guid PublicId { get; set; } = Guid.Empty;      // external-safe ID - will be set by database
        
        // ownership
        [Required] 
        public string UserId { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
        
        // fields
        [Required, StringLength(80)]  
        public string FullName { get; set; } = "";
        
        [Required, Phone]             
        public string PhoneNumber { get; set; } = "";
        
        [Required, StringLength(120)] 
        public string AddressLine1 { get; set; } = "";
        
        [StringLength(120)]           
        public string? AddressLine2 { get; set; }
        
        [Required, StringLength(60)]  
        public string City { get; set; } = "";
        
        [Required, StringLength(60)]  
        public string Province { get; set; } = "";
        
        [Required, StringLength(12)]  
        public string PostalCode { get; set; } = "";
        
        [Required, StringLength(60)]  
        public string Country { get; set; } = "South Africa";
        
        public bool IsDefault { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}