using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class ProductSpecification
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string SpecificationName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string SpecificationValue { get; set; } = string.Empty;
        
        public int DisplayOrder { get; set; } = 0;
        
        // Computed properties for backward compatibility
        public string Name => SpecificationName;
        public string Value => SpecificationValue;
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
    }
}