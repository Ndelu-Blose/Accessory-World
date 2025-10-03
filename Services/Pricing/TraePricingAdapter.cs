using System.Text.RegularExpressions;
using AccessoryWorld.Data;
using AccessoryWorld.Services.AI;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessoryWorld.Services.Pricing;

/// <summary>
/// Resolves Trae AI's detected model names to your catalog and computes a price using
/// DeviceBasePrices + PriceAdjustmentRules. Outputs ZAR (R) in PriceQuote.
/// </summary>
public sealed class TraePricingAdapter : ITraePricingAdapter
{
    private readonly ApplicationDbContext _db;

    // Common alias normalization (extend as needed)
    private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apple iphone"] = "iphone",
        ["samsung galaxy"] = "galaxy",
        ["samsung s"] = "galaxy s",
        ["iphone13"] = "iphone 13",
        ["iphone 13 5g"] = "iphone 13",
        ["iphone 13 mini"] = "iphone 13 mini",
        ["iphone 13 pro"] = "iphone 13 pro",
        ["iphone 13 pro max"] = "iphone 13 pro max",
        ["galaxy s21"] = "galaxy s21",
        ["galaxy s22"] = "galaxy s22",
        ["galaxy s23"] = "galaxy s23",
        ["pixel 6"] = "pixel 6",
        ["pixel 7"] = "pixel 7",
        ["pixel 8"] = "pixel 8"
    };

    // Strip junk like storage, colors, extra spaces, punctuation
    private static readonly Regex _storageRx = new(@"(\d+)\s?(gb|g|tb)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _punctRx = new(@"[^\w\s]", RegexOptions.Compiled);

    public TraePricingAdapter(ApplicationDbContext db) => _db = db;

    public async Task<DeviceModelCatalog?> TryResolveCatalogModelAsync(string? detectedModel, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(detectedModel)) return null;

        // 1) Normalize the incoming string
        var (normalized, storageGb) = NormalizeModel(detectedModel);

        // 2) Try exact match first
        var exact = await _db.DeviceModelCatalogs
            .FirstOrDefaultAsync(c => c.Model.ToLower() == normalized, ct);
        if (exact != null) return exact;

        // 3) Try fuzzy match by brand + model
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var brand = parts[0];
            var model = string.Join(" ", parts.Skip(1));

            var fuzzy = await _db.DeviceModelCatalogs
                .Where(c => c.Brand.ToLower().Contains(brand) && c.Model.ToLower().Contains(model))
                .FirstOrDefaultAsync(ct);
            if (fuzzy != null) return fuzzy;
        }

        // 4) Try partial match on model name
        var partial = await _db.DeviceModelCatalogs
            .Where(c => c.Model.ToLower().Contains(normalized) || normalized.Contains(c.Model.ToLower()))
            .FirstOrDefaultAsync(ct);

        return partial;
    }

    public async Task<PriceQuote?> GetPriceQuoteAsync(
        DeviceAssessmentResult assessment,
        int? storageGb = null,
        CancellationToken ct = default)
    {
        // 1) Resolve catalog model
        var catalog = await TryResolveCatalogModelAsync(assessment.DetectedModel, ct);
        if (catalog == null) return null;

        // 2) Get base price
        var basePrice = await _db.DeviceBasePrices
            .Where(p => p.DeviceModelCatalogId == catalog.Id)
            .OrderByDescending(p => p.AsOf)
            .FirstOrDefaultAsync(ct);

        if (basePrice == null) return null;

        // 3) Start with base price
        var quote = new PriceQuote
        {
            CatalogModelName = $"{catalog.Brand} {catalog.Model}",
            CatalogModelId = catalog.Id,
            BasePrice = basePrice.BasePrice,
            Currency = "R"
        };

        // 4) Apply condition-based adjustments
        ApplyConditionAdjustments(quote, assessment);

        // 5) Apply damage-based adjustments
        ApplyDamageAdjustments(quote, assessment);

        // 6) Apply storage adjustments if applicable
        if (storageGb.HasValue && assessment.DetectedStorage.HasValue)
        {
            ApplyStorageAdjustments(quote, storageGb.Value, assessment.DetectedStorage.Value);
        }

        return quote;
    }

    private static (string normalized, int? storageGb) NormalizeModel(string input)
    {
        var clean = input.Trim().ToLowerInvariant();

        // Extract storage if present
        int? storageGb = null;
        var storageMatch = _storageRx.Match(clean);
        if (storageMatch.Success && int.TryParse(storageMatch.Groups[1].Value, out var storage))
        {
            storageGb = storage;
            clean = _storageRx.Replace(clean, "").Trim();
        }

        // Remove punctuation and extra spaces
        clean = _punctRx.Replace(clean, " ");
        clean = Regex.Replace(clean, @"\s+", " ").Trim();

        // Apply aliases
        foreach (var (alias, replacement) in _aliases)
        {
            if (clean.Contains(alias.ToLowerInvariant()))
            {
                clean = clean.Replace(alias.ToLowerInvariant(), replacement);
                break;
            }
        }

        return (clean, storageGb);
    }

    private static void ApplyConditionAdjustments(PriceQuote quote, DeviceAssessmentResult assessment)
    {
        var conditionScore = assessment.OverallConditionScore;
        decimal adjustment = 0;
        string conditionGrade;

        switch (conditionScore)
        {
            case >= 0.90:
                conditionGrade = "Mint";
                adjustment = quote.BasePrice * 0.15m; // +15%
                break;
            case >= 0.75:
                conditionGrade = "Great";
                adjustment = quote.BasePrice * 0.05m; // +5%
                break;
            case >= 0.60:
                conditionGrade = "Good";
                adjustment = 0; // No adjustment
                conditionGrade = "Good";
                break;
            case >= 0.40:
                conditionGrade = "Fair";
                adjustment = quote.BasePrice * -0.25m; // -25%
                break;
            default:
                conditionGrade = "Poor";
                adjustment = quote.BasePrice * -0.50m; // -50%
                break;
        }

        quote.Breakdown[$"Condition:{conditionGrade}"] = adjustment;
        quote.TotalAdjustments += adjustment;
    }

    private static void ApplyDamageAdjustments(PriceQuote quote, DeviceAssessmentResult assessment)
    {
        // Screen damage
        if (assessment.ScreenCrackSeverity > 0)
        {
            decimal screenDeduction = assessment.ScreenCrackSeverity switch
            {
                <= 0.2 => quote.BasePrice * -0.05m, // Minor: -5%
                <= 0.5 => quote.BasePrice * -0.15m, // Moderate: -15%
                _ => quote.BasePrice * -0.35m        // Severe: -35%
            };

            var screenLevel = assessment.ScreenCrackSeverity switch
            {
                <= 0.2 => "Minor",
                <= 0.5 => "Moderate",
                _ => "Severe"
            };

            quote.Breakdown[$"Screen:{screenLevel}"] = screenDeduction;
            quote.TotalAdjustments += screenDeduction;
        }

        // Body damage
        if (assessment.BodyDentSeverity > 0)
        {
            decimal bodyDeduction = assessment.BodyDentSeverity switch
            {
                <= 0.2 => quote.BasePrice * -0.03m, // Light: -3%
                <= 0.5 => quote.BasePrice * -0.10m, // Moderate: -10%
                _ => quote.BasePrice * -0.25m       // Heavy: -25%
            };

            var bodyLevel = assessment.BodyDentSeverity switch
            {
                <= 0.2 => "Light",
                <= 0.5 => "Moderate",
                _ => "Heavy"
            };

            quote.Breakdown[$"Body:{bodyLevel}"] = bodyDeduction;
            quote.TotalAdjustments += bodyDeduction;
        }

        // Back glass damage
        if (assessment.BackGlassSeverity > 0)
        {
            decimal backGlassDeduction = assessment.BackGlassSeverity switch
            {
                <= 0.3 => quote.BasePrice * -0.08m, // Minor: -8%
                <= 0.6 => quote.BasePrice * -0.20m, // Moderate: -20%
                _ => quote.BasePrice * -0.40m       // Severe: -40%
            };

            var backGlassLevel = assessment.BackGlassSeverity switch
            {
                <= 0.3 => "Minor",
                <= 0.6 => "Moderate",
                _ => "Severe"
            };

            quote.Breakdown[$"BackGlass:{backGlassLevel}"] = backGlassDeduction;
            quote.TotalAdjustments += backGlassDeduction;
        }

        // Camera damage
        if (assessment.CameraDamageSeverity > 0)
        {
            decimal cameraDeduction = assessment.CameraDamageSeverity switch
            {
                <= 0.3 => quote.BasePrice * -0.10m, // Minor: -10%
                <= 0.6 => quote.BasePrice * -0.25m, // Moderate: -25%
                _ => quote.BasePrice * -0.50m       // Severe: -50%
            };

            var cameraLevel = assessment.CameraDamageSeverity switch
            {
                <= 0.3 => "Minor",
                <= 0.6 => "Moderate",
                _ => "Severe"
            };

            quote.Breakdown[$"Camera:{cameraLevel}"] = cameraDeduction;
            quote.TotalAdjustments += cameraDeduction;
        }

        // Water damage
        if (assessment.WaterDamageLikelihood > 0.2) // Only apply if likelihood > 20%
        {
            decimal waterDeduction = assessment.WaterDamageLikelihood switch
            {
                <= 0.4 => quote.BasePrice * -0.15m, // Possible: -15%
                <= 0.7 => quote.BasePrice * -0.35m, // Likely: -35%
                _ => quote.BasePrice * -0.70m       // Very Likely: -70%
            };

            var waterLevel = assessment.WaterDamageLikelihood switch
            {
                <= 0.4 => "Possible",
                <= 0.7 => "Likely",
                _ => "VeryLikely"
            };

            quote.Breakdown[$"Water:{waterLevel}"] = waterDeduction;
            quote.TotalAdjustments += waterDeduction;
        }
    }

    private static void ApplyStorageAdjustments(PriceQuote quote, int expectedStorage, int detectedStorage)
    {
        if (expectedStorage != detectedStorage)
        {
            // Simple storage adjustment: +/- R50 per 64GB difference
            var storageDiff = detectedStorage - expectedStorage;
            var storageAdjustment = (storageDiff / 64) * 50m;

            quote.Breakdown["Storage:Difference"] = storageAdjustment;
            quote.TotalAdjustments += storageAdjustment;
        }
    }
}