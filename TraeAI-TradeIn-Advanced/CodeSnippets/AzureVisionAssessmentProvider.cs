using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessoryWorld.Services.AI
{
    // Adjust these to match your project
    public record DeviceAssessmentRequest(string DeviceBrand, string DeviceModel, IReadOnlyList<string> PhotoPaths);
    public record DeviceAssessmentResult
    {
        public string Vendor { get; init; } = "Azure Computer Vision";
        public string VendorVersion { get; init; } = "ImageAnalysis v1";
        public double? Confidence { get; init; }
        public string Grade { get; init; } = "C";
        public string RawJson { get; init; } = "{}";
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
        public string? Caption { get; init; }
        // Optional extras
        public int? DetectedStorage { get; init; }
        public string? DetectedDamage { get; init; }
    }

    public interface IDeviceAssessmentProvider
    {
        Task<DeviceAssessmentResult> AnalyzeAsync(DeviceAssessmentRequest request, CancellationToken cancellationToken = default);
    }

    public class AzureVisionAssessmentProvider : IDeviceAssessmentProvider
    {
        private readonly AzureVisionOptions _options;
        private readonly ILogger<AzureVisionAssessmentProvider> _logger;

        public AzureVisionAssessmentProvider(IOptions<AzureVisionOptions> options, ILogger<AzureVisionAssessmentProvider> logger)
        {
            _options = options.Value ?? new AzureVisionOptions();
            _logger = logger;
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(DeviceAssessmentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.Key))
                throw new InvalidOperationException("AzureVision options are missing Endpoint/Key.");

            var serviceOptions = new VisionServiceOptions(new Uri(_options.Endpoint), new AzureKeyCredential(_options.Key));
            var features = ImageAnalysisFeature.Captions | ImageAnalysisFeature.Tags;

            var allTags = new List<string>();
            string? bestCaption = null;
            double? avgConfidence = null;
            var confidences = new List<double>();
            var rawPayload = new List<object>();

            foreach (var path in request.PhotoPaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                ImageSource imageSource;
                if (Uri.TryCreate(path, UriKind.Absolute, out var abs))
                {
                    imageSource = ImageSource.FromUri(abs);
                }
                else if (File.Exists(path))
                {
                    imageSource = ImageSource.FromFile(path);
                }
                else
                {
                    // try as absolute URL by building from path (caller should pass absolute)
                    throw new FileNotFoundException($"Photo not found or not an absolute URL: {path}");
                }

                using var analyzer = new ImageAnalyzer(serviceOptions, imageSource, features);
                var result = await analyzer.AnalyzeAsync(cancellationToken);

                var tags = result?.Tags?.Values?.Select(t => t.Name)?.ToList() ?? new List<string>();
                allTags.AddRange(tags);

                if (result?.Captions?.Values is { Count: > 0 })
                {
                    var cap = result.Captions.Values.OrderByDescending(c => c.Confidence).First();
                    if (bestCaption == null || cap.Confidence > (avgConfidence ?? 0))
                        bestCaption = cap.Text;
                    confidences.Add(cap.Confidence);
                }

                // minimal raw capture (do not store entire SDK object)
                rawPayload.Add(new
                {
                    Caption = result?.Captions?.Values?.Select(c => new { c.Text, c.Confidence }),
                    Tags = result?.Tags?.Values?.Select(t => new { t.Name, t.Confidence })
                });
            }

            if (confidences.Count > 0)
                avgConfidence = confidences.Average();

            var deducedDamage = DeduceDamage(allTags);
            var grade = MapToGrade(allTags, deducedDamage);

            var rawJson = JsonSerializer.Serialize(rawPayload);

            return new DeviceAssessmentResult
            {
                Vendor = "Azure Computer Vision",
                VendorVersion = "ImageAnalysis v1",
                Confidence = avgConfidence,
                Grade = grade,
                RawJson = rawJson,
                Tags = allTags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                Caption = bestCaption,
                DetectedDamage = deducedDamage
            };
        }

        private static string? DeduceDamage(IEnumerable<string> tags)
        {
            var set = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));
            if (set.Contains("crack") || set.Contains("cracked") || set.Contains("broken screen") || set.Contains("damaged"))
                return "screen_cracked";
            if (set.Contains("scratch") || set.Contains("scratched"))
                return "scratched";
            if (set.Contains("dent") || set.Contains("dented"))
                return "dented";
            return null;
        }

        private static string MapToGrade(IEnumerable<string> tags, string? damage)
        {
            var s = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));
            bool severe = s.Contains("crack") || s.Contains("cracked") || s.Contains("broken screen") || s.Contains("heavy damage");
            bool light = s.Contains("scratch") || s.Contains("scuffed") || s.Contains("worn");

            if (severe || damage == "screen_cracked") return "D";
            if (light) return "C";
            return "B"; // optimistic default; adjust to your policy
        }
    }
}
