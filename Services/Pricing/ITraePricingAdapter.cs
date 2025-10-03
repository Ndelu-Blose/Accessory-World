using AccessoryWorld.Services.AI;
using AccessoryWorld.Models;

namespace AccessoryWorld.Services.Pricing;

public interface ITraePricingAdapter
{
    /// <summary>
    /// Try to resolve a catalog model from a free-form model string (from AI).
    /// </summary>
    Task<DeviceModelCatalog?> TryResolveCatalogModelAsync(
        string? detectedModel,
        CancellationToken ct = default);

    /// <summary>
    /// Produce a price quote using AI assessment + catalog + rules.
    /// </summary>
    Task<PriceQuote?> GetPriceQuoteAsync(
        DeviceAssessmentResult assessment,
        int? storageGb = null,
        CancellationToken ct = default);
}