using AccessoryWorld.Services;
using AccessoryWorld.Services.AI;
using AccessoryWorld.Services.Pricing;
using Microsoft.AspNetCore.Mvc;

namespace AccessoryWorld.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly PricingService _pricingService;
        private readonly ITraePricingAdapter _pricingAdapter;
        private readonly ILogger<TestController> _logger;

        public TestController(PricingService pricingService, ITraePricingAdapter pricingAdapter, ILogger<TestController> logger)
        {
            _pricingService = pricingService;
            _pricingAdapter = pricingAdapter;
            _logger = logger;
        }

        [HttpPost("seed-pricing-data")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SeedPricingData()
        {
            try
            {
                await _pricingService.SeedTestDataAsync();
                _logger.LogInformation("Pricing data seeded successfully");
                return Ok(new { message = "Pricing data seeded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed pricing data");
                return StatusCode(500, new { error = "Failed to seed pricing data", details = ex.Message });
            }
        }

        [HttpPost("test-pricing-adapter")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TestPricingAdapter()
        {
            try
            {
                _logger.LogInformation("Testing TraePricingAdapter with existing catalog and pricing data");

                // Test 1: Resolve catalog model
                var detectedModel = "iPhone 13";
                var catalogModel = await _pricingAdapter.TryResolveCatalogModelAsync(detectedModel);
                
                if (catalogModel == null)
                {
                    return BadRequest(new { error = $"Failed to resolve '{detectedModel}' in catalog" });
                }

                // Test 2: Generate price quote
                var mockAssessment = new DeviceAssessmentResult
                {
                    DetectedBrand = "Apple",
                    DetectedModel = "iPhone 13",
                    DetectedType = "Smartphone",
                    DetectedStorage = 128,
                    OverallConditionScore = 0.85, // Good condition
                    ScreenCrackSeverity = 0.1,
                    BodyDentSeverity = 0.05,
                    BackGlassSeverity = 0.0,
                    CameraDamageSeverity = 0.0,
                    WaterDamageLikelihood = 0.0,
                    IdentificationConfidence = 0.9,
                    ModelVersion = "Test",
                    ProcessingTimeMs = 100,
                    FunctionalIssues = new List<string>(),
                    CosmeticIssues = new List<string> { "Minor scratches" },
                    DetectedDamage = new List<DetectedDamage>()
                };

                var priceQuote = await _pricingAdapter.GetPriceQuoteAsync(mockAssessment);
                
                if (priceQuote == null)
                {
                    return BadRequest(new { error = "Failed to generate price quote" });
                }

                // Test 3: Test model variations
                var modelVariations = new[] { "iPhone13", "iphone 13", "IPHONE 13", "Apple iPhone 13" };
                var variationResults = new List<object>();
                
                foreach (var variation in modelVariations)
                {
                    var resolved = await _pricingAdapter.TryResolveCatalogModelAsync(variation);
                    variationResults.Add(new { 
                        variation, 
                        resolved = resolved != null,
                        catalogId = resolved?.Id
                    });
                }

                _logger.LogInformation("Pricing adapter test completed successfully");

                return Ok(new
                {
                    message = "Pricing adapter test completed successfully",
                    catalogModel = new
                    {
                        id = catalogModel.Id,
                        brand = catalogModel.Brand,
                        model = catalogModel.Model,
                        deviceType = catalogModel.DeviceType,
                        storageGb = catalogModel.StorageGb
                    },
                    priceQuote = new
                    {
                        catalogModelName = priceQuote.CatalogModelName,
                        catalogModelId = priceQuote.CatalogModelId,
                        basePrice = priceQuote.BasePrice,
                        totalAdjustments = priceQuote.TotalAdjustments,
                        finalPrice = priceQuote.FinalPrice,
                        currency = priceQuote.Currency,
                        breakdown = priceQuote.Breakdown
                    },
                    modelVariations = variationResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test pricing adapter");
                return StatusCode(500, new { error = "Failed to test pricing adapter", details = ex.Message });
            }
        }
    }
}