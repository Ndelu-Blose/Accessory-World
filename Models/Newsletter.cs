using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class Newsletter
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsActive { get; set; } = true;
        
        [MaxLength(50)]
        public string? Source { get; set; } // Track where subscription came from
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        public DateTime? UnsubscribedAt { get; set; }
    }
}