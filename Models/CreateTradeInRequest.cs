using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Models
{
    public class CreateTradeInRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Device brand is required")]
        [Display(Name = "Device Brand")]
        public string DeviceBrand { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Device model is required")]
        [Display(Name = "Device Model")]
        public string DeviceModel { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Device type is required")]
        [Display(Name = "Device Type")]
        public string DeviceType { get; set; } = string.Empty;
        
        [Display(Name = "IMEI/Serial Number")]
        public string? IMEI { get; set; }
        
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        [Required(ErrorMessage = "Condition grade is required")]
        [Display(Name = "Condition Grade")]
        public string ConditionGrade { get; set; } = string.Empty;
        
        [Display(Name = "Photos")]
        public List<string>? Photos { get; set; }
        
        [Display(Name = "Proposed Value")]
        [Range(0, double.MaxValue, ErrorMessage = "Proposed value must be a positive number")]
        public decimal? ProposedValue { get; set; }
    }
}