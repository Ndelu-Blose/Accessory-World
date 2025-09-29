using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AccessoryWorld.Models.ViewModels
{
    public class CreateTradeInRequest
    {
        [Required] public required string FullName { get; set; }
        [Required, EmailAddress] public required string Email { get; set; }
        [Required] public required string Phone { get; set; }

        // Match every input rendered in the form:
        public required string DeviceBrand { get; set; }
        public required string DeviceModel { get; set; }
        public required string DeviceType { get; set; }      // <- was missing
        public required string Description { get; set; }     // <- was missing
        
        // Additional properties referenced in controller
        public string? ConditionGrade { get; set; }
        public decimal? ProposedValue { get; set; }

        // Photos upload
        public List<IFormFile> Photos { get; set; } = new();
    }
}