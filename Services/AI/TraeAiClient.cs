using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AccessoryWorld.Services.AI;

public interface ITraeAiClient
{
    Task<TraeAssessmentResponse> AssessAsync(TraeAssessmentRequest req, CancellationToken ct);
}

public sealed class TraeAiClient(HttpClient http, TraeAiOptions opt) : ITraeAiClient
{
    private readonly HttpClient _http = http;
    private readonly TraeAiOptions _opt = opt;

    public async Task<TraeAssessmentResponse> AssessAsync(TraeAssessmentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opt.BaseAddress))
            throw new InvalidOperationException("TraeAI.BaseAddress is not configured.");

        _http.BaseAddress ??= new Uri(_opt.BaseAddress, UriKind.Absolute);
        if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

        var json = JsonSerializer.Serialize(req);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var res = await _http.PostAsync("api/assess", content, ct);
        res.EnsureSuccessStatusCode();
        var payload = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<TraeAssessmentResponse>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new TraeAssessmentResponse();
    }
}

// Request/Response models (adapt if your Trae API differs)
public sealed class TraeAssessmentRequest
{
    public string? TradeInPublicId { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public List<string> ImageUrls { get; set; } = [];
    public string? ExpectedBrand { get; set; }
    public string? ExpectedModel { get; set; }
}

public sealed class TraeAssessmentResponse
{
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public double? Confidence { get; set; }                // 0..1
    public string? DetectedBrand { get; set; }
    public string? DetectedModel { get; set; }             // e.g., "iPhone 13"
    public string? DetectedType { get; set; }
    public int? Storage { get; set; }
    public double OverallCondition { get; set; }           // 0..1
    public double ScreenCondition { get; set; }            // 0..1
    public double BodyCondition { get; set; }              // 0..1
    public double BackGlassCondition { get; set; }         // 0..1
    public double CameraCondition { get; set; }            // 0..1
    public double WaterDamageRisk { get; set; }            // 0..1
    public List<string> FunctionalIssues { get; set; } = new();
    public List<string> CosmeticIssues { get; set; } = new();
    public int ProcessingTimeMs { get; set; }
    public string? ModelVersion { get; set; }
    
    // Legacy properties for backward compatibility
    public double OverallConditionScore { get; set; }      // 0..1
    public double ScreenCrackSeverity { get; set; }        // 0..1
    public double BodyDentSeverity { get; set; }           // 0..1
    public double WaterDamageLikelihood { get; set; }      // 0..1
    public string? Notes { get; set; }
    public string? ModelDetected { get; set; }             // e.g., "iPhone 13"
}