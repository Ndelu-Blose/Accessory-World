using System.Text.Json.Serialization;

namespace AccessoryWorld.Services.AI
{
    /// <summary>
    /// Result from AI device assessment analysis
    /// </summary>
    public sealed class DeviceAssessmentResult
    {
        // Device identification
        [JsonPropertyName("detected_brand")]
        public string? DetectedBrand { get; set; }
        
        [JsonPropertyName("detected_model")]
        public string? DetectedModel { get; set; }
        
        [JsonPropertyName("detected_type")]
        public string? DetectedType { get; set; }
        
        [JsonPropertyName("detected_storage")]
        public int? DetectedStorage { get; set; }
        
        [JsonPropertyName("identification_confidence")]
        public double IdentificationConfidence { get; set; }
        
        // Damage assessment (0.0 = perfect, 1.0 = severely damaged)
        [JsonPropertyName("screen_crack_severity")]
        public double ScreenCrackSeverity { get; set; }
        
        [JsonPropertyName("body_dent_severity")]
        public double BodyDentSeverity { get; set; }
        
        [JsonPropertyName("back_glass_severity")]
        public double BackGlassSeverity { get; set; }
        
        [JsonPropertyName("camera_damage_severity")]
        public double CameraDamageSeverity { get; set; }
        
        [JsonPropertyName("water_damage_likelihood")]
        public double WaterDamageLikelihood { get; set; }
        
        // Additional assessments
        [JsonPropertyName("overall_condition_score")]
        public double OverallConditionScore { get; set; }
        
        [JsonPropertyName("functional_issues")]
        public List<string> FunctionalIssues { get; set; } = new();
        
        [JsonPropertyName("cosmetic_issues")]
        public List<string> CosmeticIssues { get; set; } = new();
        
        [JsonPropertyName("detected_damage")]
        public List<DetectedDamage> DetectedDamage { get; set; } = new();
        
        // Metadata
        [JsonPropertyName("analysis_timestamp")]
        public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
        
        [JsonPropertyName("model_version")]
        public string ModelVersion { get; set; } = string.Empty;
        
        [JsonPropertyName("processing_time_ms")]
        public int ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// Detected damage item
    /// </summary>
    public sealed class DetectedDamage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
        
        [JsonPropertyName("severity")]
        public double Severity { get; set; }
        
        [JsonPropertyName("location")]
        public string? Location { get; set; }
    }

    /// <summary>
    /// Request payload for AI device assessment
    /// </summary>
    public sealed class DeviceAssessmentRequest
    {
        [JsonPropertyName("image_urls")]
        public List<string> ImageUrls { get; set; } = new();
        
        [JsonPropertyName("device_brand")]
        public string? DeviceBrand { get; set; }
        
        [JsonPropertyName("device_model")]
        public string? DeviceModel { get; set; }
        
        [JsonPropertyName("device_type")]
        public string? DeviceType { get; set; }
        
        [JsonPropertyName("additional_context")]
        public string? AdditionalContext { get; set; }
    }

    /// <summary>
    /// Response from Trae AI API
    /// </summary>
    public sealed class TraeAiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("assessment")]
        public DeviceAssessmentResult? Assessment { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
        
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
    }
}