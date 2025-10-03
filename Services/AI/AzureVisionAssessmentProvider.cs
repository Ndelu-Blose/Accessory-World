using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessoryWorld.Services.AI
{
    public class AzureVisionAssessmentProvider : IDeviceAssessmentProvider
    {
        private readonly AzureVisionOptions _options;
        private readonly ILogger<AzureVisionAssessmentProvider> _logger;

        public string ProviderName => "azure-vision";
        public string ModelVersion => "ImageAnalysis v1.0";

        public AzureVisionAssessmentProvider(IOptions<AzureVisionOptions> options, ILogger<AzureVisionAssessmentProvider> logger)
        {
            _options = options.Value ?? new AzureVisionOptions();
            _logger = logger;
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(List<string> imageUrls, CancellationToken cancellationToken = default)
        {
            var request = new DeviceAssessmentRequest
            {
                DeviceBrand = "Unknown",
                DeviceModel = "Unknown",
                ImageUrls = imageUrls
            };
            return await AnalyzeAsync(request, cancellationToken);
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(DeviceAssessmentRequest request, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Create the client
                var client = new ImageAnalysisClient(
                    new Uri(_options.Endpoint ?? throw new InvalidOperationException("Azure Vision endpoint is not configured")),
                    new AzureKeyCredential(_options.Key ?? throw new InvalidOperationException("Azure Vision API key is not configured")));

                // Analyze the first image URL (simplified for demo)
                if (request.ImageUrls == null || !request.ImageUrls.Any())
                {
                    throw new ArgumentException("No image URLs provided");
                }

                var imageUrl = request.ImageUrls.FirstOrDefault();

                var analysisResult = await client.AnalyzeAsync(
                    BinaryData.FromObjectAsJson(new { url = imageUrl }),
                    VisualFeatures.Tags | VisualFeatures.Caption,
                    new ImageAnalysisOptions { GenderNeutralCaption = true });

                // Extract tags and analyze for damage
                var detectedDamages = new List<DetectedDamage>();
                var avgConfidence = 0.0;

                if (analysisResult.Value.Tags != null)
                {
                    var tags = analysisResult.Value.Tags.Values;
                    detectedDamages = DeduceDamage(tags);
                    avgConfidence = tags.Any() ? tags.Average(t => t.Confidence) : 0.0;
                }

                return new DeviceAssessmentResult
                {
                    DetectedBrand = "Unknown", // Azure Vision doesn't detect brand/model
                    DetectedModel = "Unknown",
                    DetectedType = "Unknown",
                    IdentificationConfidence = avgConfidence,
                    ScreenCrackSeverity = detectedDamages.Any(d => d.Type.Contains("crack", StringComparison.OrdinalIgnoreCase)) ? 0.8 : 0.0,
                    BodyDentSeverity = detectedDamages.Any(d => d.Type.Contains("dent", StringComparison.OrdinalIgnoreCase)) ? 0.6 : 0.0,
                    BackGlassSeverity = detectedDamages.Any(d => d.Type.Contains("glass", StringComparison.OrdinalIgnoreCase)) ? 0.7 : 0.0,
                    CameraDamageSeverity = detectedDamages.Any(d => d.Type.Contains("camera", StringComparison.OrdinalIgnoreCase)) ? 0.5 : 0.0,
                    WaterDamageLikelihood = detectedDamages.Any(d => d.Type.Contains("water", StringComparison.OrdinalIgnoreCase)) ? 0.9 : 0.0,
                    OverallConditionScore = CalculateOverallConditionScore(detectedDamages),
                    FunctionalIssues = detectedDamages.Where(d => d.Type.Contains("functional", StringComparison.OrdinalIgnoreCase))
                                            .Select(d => d.Type).ToList(),
                    CosmeticIssues = detectedDamages.Where(d => !d.Type.Contains("functional", StringComparison.OrdinalIgnoreCase))
                                           .Select(d => d.Type).ToList(),
                    DetectedDamage = detectedDamages,
                    AnalysisTimestamp = DateTime.UtcNow,
                    ModelVersion = "AzureVision-1.0",
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing device with Azure Vision");
                throw;
            }
        }

        private List<DetectedDamage> DeduceDamage(IReadOnlyList<DetectedTag> tags)
        {
            var damages = new List<DetectedDamage>();
            
            foreach (var tag in tags)
            {
                if (IsDamageRelated(tag.Name))
                {
                    damages.Add(new DetectedDamage
                {
                    Type = tag.Name,
                    Confidence = tag.Confidence,
                    Severity = tag.Confidence, // Using confidence as severity approximation
                    Location = "Unknown"
                });
                }
            }
            
            return damages;
        }

        private DetectedDamage? AnalyzeDamageFromTags(List<string> tags)
        {
            var tagSet = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));
            
            // Check for screen damage
            if (tagSet.Any(t => t.Contains("crack") || t.Contains("broken") || t.Contains("shatter")))
            {
                return new DetectedDamage
                {
                    Type = "screen_damage",
                    Severity = 0.8,
                    Confidence = 0.8,
                    Location = "Screen"
                };
            }

            // Check for scratches
            if (tagSet.Any(t => t.Contains("scratch") || t.Contains("scuff")))
            {
                return new DetectedDamage
                {
                    Type = "surface_damage",
                    Severity = 0.3,
                    Confidence = 0.7,
                    Location = "Surface"
                };
            }

            // Check for dents
            if (tagSet.Any(t => t.Contains("dent") || t.Contains("bent")))
            {
                return new DetectedDamage
                {
                    Type = "physical_damage",
                    Severity = 0.6,
                    Confidence = 0.75,
                    Location = "Body"
                };
            }

            return null;
        }

        private bool IsDamageRelated(string tagName)
        {
            var damageKeywords = new[] { "crack", "broken", "shatter", "scratch", "scuff", "dent", "bent", "damage" };
            return damageKeywords.Any(keyword => tagName.ToLowerInvariant().Contains(keyword));
        }

        private double CalculateOverallConditionScore(List<DetectedDamage> damages)
        {
            if (!damages.Any())
                return 1.0; // Perfect condition
            
            // Calculate average severity and invert it (1.0 - severity = condition score)
            var averageSeverity = damages.Average(d => d.Severity);
            return Math.Max(0.0, 1.0 - averageSeverity);
        }
    }
}