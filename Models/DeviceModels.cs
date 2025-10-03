using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessoryWorld.Models
{
    public class DeviceModelCatalog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } = default!;
        
        [Required]
        [MaxLength(200)]
        public string Model { get; set; } = default!;
        
        [Required]
        [MaxLength(50)]
        public string DeviceType { get; set; } = default!; // Smartphone, Tablet, etc.
        
        public int ReleaseYear { get; set; }
        
        public int? StorageGb { get; set; } // nullable if N/A
        
        // Navigation properties
        public virtual ICollection<DeviceBasePrice> BasePrices { get; set; } = new List<DeviceBasePrice>();
    }

    public class DeviceBasePrice
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int DeviceModelCatalogId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; } // pristine price today
        
        public DateTime AsOf { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual DeviceModelCatalog DeviceModel { get; set; } = default!;
    }

    public class PriceAdjustmentRule
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!; // e.g., "CRACKED_SCREEN_MINOR"
        
        [Required]
        [Column(TypeName = "decimal(5,4)")]
        public decimal Multiplier { get; set; } // 0.85m = -15%
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? FlatDeduction { get; set; } // optional absolute deduction
        
        [MaxLength(100)]
        public string AppliesTo { get; set; } = "ANY"; // Brand/Type conditions if needed
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}