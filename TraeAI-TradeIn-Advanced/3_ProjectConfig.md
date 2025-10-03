# 3) Project configuration (NuGet, settings, DI)

## NuGet
```
dotnet add package Azure.AI.Vision.ImageAnalysis
```

## appsettings.json
```jsonc
{
  "AzureVision": {
    "Endpoint": "https://YOUR-RESOURCE-NAME.cognitiveservices.azure.com/",
    "Key": "YOUR_KEY",
    // Optional tuning
    "DefaultConfidenceCutoff": 0.6
  }
}
```

## Program.cs (DI)
```csharp
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Options;
using AccessoryWorld.Services.AI;

// Bind options
builder.Services.Configure<AzureVisionOptions>(
    builder.Configuration.GetSection("AzureVision"));

// Register the provider as your IDeviceAssessmentProvider (or the interface you use)
builder.Services.AddSingleton<IDeviceAssessmentProvider, AzureVisionAssessmentProvider>();
```

> Ensure `IDeviceAssessmentProvider` is the same interface your `TradeInAssessmentWorker` expects. Rename if needed.
