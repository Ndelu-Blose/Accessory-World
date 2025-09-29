using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class ShippingCalculationRequest
    {
        [Required]
        public Guid AddressId { get; set; }
        
        [Required]
        public string FulfillmentMethod { get; set; } = "delivery";
        
        public decimal SubTotal { get; set; }
    }
}