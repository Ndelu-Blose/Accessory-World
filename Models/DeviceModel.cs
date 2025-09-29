using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class DeviceModel
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(64)]
        public string Brand { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(128)]
        public string Model { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseTradeInValue { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}