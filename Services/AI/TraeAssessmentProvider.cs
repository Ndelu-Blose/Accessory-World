using Microsoft.Extensions.Options;

namespace AccessoryWorld.Services.AI;

public sealed class TraeAssessmentProvider : IDeviceAssessmentProvider
{
    private readonly ITraeAiClient _client;
    private readonly TraeAiOptions _opt;


    public TraeAssessmentProvider(ITraeAiClient client, IOptions<TraeAiOptions> opt)
    {
        _client = client;
        _opt = opt.Value;
    }

    public string ProviderName => "TraeAI";
    public string ModelVersion => "v1";

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

    public async Task<DeviceAssessmentResult> AnalyzeAsync(
        DeviceAssessmentRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Map your request -> Trae
        var req = new TraeAssessmentRequest
        {
            TradeInPublicId = request.AdditionalContext, // Use additional context as trade-in ID
            PhotoUrls = request.ImageUrls?.ToList() ?? []
        };

        TraeAssessmentResponse res;
        try
        {
            res = await _client.AssessAsync(req, cancellationToken);
        }
        catch (Exception ex)
        {
            // graceful fallback – return neutral/low-confidence result
            return new DeviceAssessmentResult
            {
                DetectedBrand = "Unknown",
                DetectedModel = "Unknown",
                DetectedType = "Unknown",
                IdentificationConfidence = 0.0,
                OverallConditionScore = 0.5,
                ScreenCrackSeverity = 0.0,
                BodyDentSeverity = 0.0,
                BackGlassSeverity = 0.0,
                CameraDamageSeverity = 0.0,
                WaterDamageLikelihood = 0.0,
                ModelVersion = ModelVersion,
                ProcessingTimeMs = 0,
                FunctionalIssues = new List<string> { "Trae assessment failed: " + ex.Message }
            };
        }

        // Map Trae -> generic result
        var conf = res.Confidence ?? _opt.DefaultConfidenceCutoff;

        return new DeviceAssessmentResult
        {
            DetectedBrand = ExtractBrand(res.ModelDetected),
            DetectedModel = res.ModelDetected ?? "Unknown",
            DetectedType = "Smartphone", // Default assumption
            IdentificationConfidence = conf,
            OverallConditionScore = res.OverallConditionScore,
            ScreenCrackSeverity = res.ScreenCrackSeverity,
            BodyDentSeverity = res.BodyDentSeverity,
            BackGlassSeverity = 0.0, // Not provided by Trae response
            CameraDamageSeverity = 0.0, // Not provided by Trae response
            WaterDamageLikelihood = res.WaterDamageLikelihood,
            ModelVersion = ModelVersion,
            ProcessingTimeMs = 1000, // Estimated
            FunctionalIssues = string.IsNullOrEmpty(res.Notes) ? new List<string>() : new List<string> { res.Notes },
            CosmeticIssues = BuildCosmeticIssues(res),
            DetectedDamage = BuildDetectedDamage(res)
        };
    }

    private static string ExtractBrand(string? modelDetected)
    {
        if (string.IsNullOrWhiteSpace(modelDetected))
            return "Unknown";

        var lower = modelDetected.ToLowerInvariant();
        if (lower.Contains("iphone") || lower.Contains("apple"))
            return "Apple";
        if (lower.Contains("galaxy") || lower.Contains("samsung"))
            return "Samsung";
        if (lower.Contains("pixel") || lower.Contains("google"))
            return "Google";
        if (lower.Contains("huawei"))
            return "Huawei";
        if (lower.Contains("xiaomi"))
            return "Xiaomi";
        if (lower.Contains("oneplus"))
            return "OnePlus";

        return "Unknown";
    }

    private static List<string> BuildCosmeticIssues(TraeAssessmentResponse res)
    {
        var issues = new List<string>();
        
        if (res.ScreenCrackSeverity > 0.1)
            issues.Add($"Screen damage (severity: {res.ScreenCrackSeverity:F2})");
        
        if (res.BodyDentSeverity > 0.1)
            issues.Add($"Body damage (severity: {res.BodyDentSeverity:F2})");
        
        if (res.WaterDamageLikelihood > 0.3)
            issues.Add($"Possible water damage (likelihood: {res.WaterDamageLikelihood:F2})");

        return issues;
    }

    private static List<DetectedDamage> BuildDetectedDamage(TraeAssessmentResponse res)
    {
        var damage = new List<DetectedDamage>();
        
        if (res.ScreenCrackSeverity > 0.0)
        {
            damage.Add(new DetectedDamage
            {
                Type = "Screen Crack",
                Confidence = res.Confidence ?? 0.8,
                Severity = res.ScreenCrackSeverity,
                Location = "Screen"
            });
        }
        
        if (res.BodyDentSeverity > 0.0)
        {
            damage.Add(new DetectedDamage
            {
                Type = "Body Dent",
                Confidence = res.Confidence ?? 0.8,
                Severity = res.BodyDentSeverity,
                Location = "Body"
            });
        }
        
        if (res.WaterDamageLikelihood > 0.3)
        {
            damage.Add(new DetectedDamage
            {
                Type = "Water Damage",
                Confidence = res.Confidence ?? 0.6,
                Severity = res.WaterDamageLikelihood,
                Location = "Internal"
            });
        }

        return damage;
    }
}