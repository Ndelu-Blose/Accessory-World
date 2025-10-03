namespace AccessoryWorld.Services.AI;

public sealed class TraeAiOptions
{
    public string? BaseAddress { get; set; }
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "trae-v1";
    public double DefaultConfidenceCutoff { get; set; } = 0.6;
}