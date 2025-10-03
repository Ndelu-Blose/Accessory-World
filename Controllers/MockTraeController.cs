using Microsoft.AspNetCore.Mvc;
using AccessoryWorld.Services.AI;

namespace AccessoryWorld.Controllers;

[ApiController]
[Route("api/mock-trae")]
public class MockTraeController : ControllerBase
{
    private readonly ILogger<MockTraeController> _logger;

    public MockTraeController(ILogger<MockTraeController> logger)
    {
        _logger = logger;
    }

    [HttpPost("assess")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Assess([FromBody] TraeAssessmentRequest request)
    {
        _logger.LogInformation("MockTrae Assess called with request: {@Request}", request);
        
        if (request == null)
        {
            _logger.LogWarning("Request is null");
            return BadRequest("Request cannot be null");
        }

        // Simulate processing delay
        await Task.Delay(Random.Shared.Next(500, 2000));

        // Generate deterministic but varied responses based on input
        var hash = request.ImageUrls?.Count ?? 0;
        var baseConfidence = 0.8 + (hash % 3) * 0.05;
        var conditionScore = 0.7 + (hash % 4) * 0.1;

        var response = new TraeAssessmentResponse
        {
            Success = true,
            Confidence = Math.Clamp(baseConfidence, 0.75, 0.95),
            DetectedBrand = request.ExpectedBrand ?? "Apple",
            DetectedModel = request.ExpectedModel ?? "iPhone 13",
            DetectedType = "Smartphone",
            Storage = 128,
            OverallCondition = Math.Clamp(conditionScore, 0.6, 0.9),
            ScreenCondition = 0.85,
            BodyCondition = 0.8,
            BackGlassCondition = 0.9,
            CameraCondition = 0.95,
            WaterDamageRisk = 0.1,
            FunctionalIssues = new List<string>(),
            CosmeticIssues = new List<string> { "Minor wear on edges" },
            ProcessingTimeMs = Random.Shared.Next(800, 1500),
            ModelVersion = "mock-1.0"
        };

        // Occasionally simulate errors for testing
        if (hash % 10 == 0)
        {
            response.Success = false;
            response.ErrorMessage = "Mock processing error for testing";
        }

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}