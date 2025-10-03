namespace AccessoryWorld.Services.Pricing;

public sealed class PriceQuote
{
    public string CatalogModelName { get; set; } = "";
    public int CatalogModelId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal FinalPrice => BasePrice + TotalAdjustments;
    public Dictionary<string, decimal> Breakdown { get; set; } = new();
    // South African Rand (ZAR)
    public string Currency { get; set; } = "R";
}