using System;

namespace AccessoryWorld.Services.AI
{
    public class AzureVisionOptions
    {
        public string? Endpoint { get; set; }
        public string? Key { get; set; }
        public double DefaultConfidenceCutoff { get; set; } = 0.6;
    }
}