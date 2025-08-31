using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.DTOs
{
    public class AddToCartDto
    {
        [Required(ErrorMessage = "SKU code is required")]
        [StringLength(50, ErrorMessage = "SKU code cannot exceed 50 characters")]
        public string SkuCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; }
    }
}