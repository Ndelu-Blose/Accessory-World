namespace AccessoryWorld.Services.AI;

public sealed class StubAssessmentProvider : IDeviceAssessmentProvider
{
    public string ProviderName => "StubTrae";
    public string ModelVersion => "dev";

    public async Task<DeviceAssessmentResult> AnalyzeAsync(
        List<string> imageUrls, 
        CancellationToken cancellationToken = default)
    {
        var request = new DeviceAssessmentRequest
        {
            ImageUrls = imageUrls
        };

        return await AnalyzeAsync(request, cancellationToken);
    }

    public Task<DeviceAssessmentResult> AnalyzeAsync(
        DeviceAssessmentRequest request, 
        CancellationToken cancellationToken = default)
    {
        // deterministic but realistic-ish values
        var hash = (request.ImageUrls?.Count ?? 0) + (request.AdditionalContext?.Length ?? 0);
        var baseScore = 0.75 - (hash % 3) * 0.1;

        var result = new DeviceAssessmentResult
        {
            DetectedBrand = "Apple",
            DetectedModel = "iPhone 13",
            DetectedType = "Smartphone",
            DetectedStorage = 128,
            IdentificationConfidence = 0.9,
            OverallConditionScore = Math.Clamp(baseScore, 0.3, 0.95),
            ScreenCrackSeverity = 0.1,
            BodyDentSeverity = 0.15,
            BackGlassSeverity = 0.05,
            CameraDamageSeverity = 0.0,
            WaterDamageLikelihood = 0.05,
            ModelVersion = ModelVersion,
            ProcessingTimeMs = 500,
            FunctionalIssues = new List<string>(),
            CosmeticIssues = new List<string> { "Minor scratches on body" },
            DetectedDamage = new List<DetectedDamage>
            {
                new DetectedDamage
                {
                    Type = "Minor Scratches",
                    Confidence = 0.8,
                    Severity = 0.15,
                    Location = "Body"
                }
            }
        };

        return Task.FromResult(result);
    }
}