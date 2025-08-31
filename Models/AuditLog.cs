using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // Order, Payment, TradeIn, etc.
        
        [Required]
        [MaxLength(50)]
        public string EntityId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE, etc.
        
        [MaxLength(100)]
        public string? UserId { get; set; }
        
        [MaxLength(100)]
        public string? UserEmail { get; set; }
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        public string? OldValues { get; set; } // JSON
        public string? NewValues { get; set; } // JSON
        
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ApplicationUser? User { get; set; }
    }
    
    public class Role
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
    
    public class Permission
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // ORDERS, INVENTORY, USERS, etc.
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
    
    public class UserRole
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public int RoleId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(450)]
        public string? AssignedByUserId { get; set; }
        
        // Navigation properties
        public virtual ApplicationUser User { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual ApplicationUser? AssignedByUser { get; set; }
    }
    
    public class RolePermission
    {
        public int Id { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        
        [Required]
        public int PermissionId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Role Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}