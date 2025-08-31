using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.ViewModels
{
    public class ShippingCalculationRequest
    {
        [Required]
        public int AddressId { get; set; }
        
        [Required]
        public string FulfillmentMethod { get; set; } = "delivery";
        
        public decimal SubTotal { get; set; }
    }
}