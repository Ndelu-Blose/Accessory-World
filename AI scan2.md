1) Make sure Azure Vision is the active provider

In appsettings.Development.json:

"TradeInSettings": {
  "AI": { "Provider": "AzureVision" }
}


Restart the app so DI rebinds to AzureVisionAssessmentProvider.

2) Create the pricing catalog tables (one-time)

If you haven’t already run these, execute against your AccessoryWorld DB:

IF OBJECT_ID('dbo.DeviceModelCatalogs','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceModelCatalogs(
    Id int IDENTITY(1,1) PRIMARY KEY,
    Brand nvarchar(64) NOT NULL,
    Model nvarchar(128) NOT NULL,
    DeviceType nvarchar(32) NOT NULL,
    ReleaseYear int NOT NULL,
    StorageGb int NULL
  );
  CREATE UNIQUE INDEX IX_DeviceModelCatalogs_Brand_Model_Type
    ON dbo.DeviceModelCatalogs(Brand, Model, DeviceType);
END;

IF OBJECT_ID('dbo.DeviceBasePrices','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceBasePrices(
    Id int IDENTITY(1,1) PRIMARY KEY,
    DeviceModelCatalogId int NOT NULL
      FOREIGN KEY REFERENCES dbo.DeviceModelCatalogs(Id) ON DELETE CASCADE,
    BasePrice decimal(18,2) NOT NULL,
    AsOf datetime2 NOT NULL DEFAULT(sysutcdatetime())
  );
END;

IF OBJECT_ID('dbo.PriceAdjustmentRules','U') IS NULL
BEGIN
  CREATE TABLE dbo.PriceAdjustmentRules(
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(64) NOT NULL,
    Multiplier decimal(9,4) NOT NULL,
    FlatDeduction decimal(18,2) NULL,
    AppliesTo nvarchar(32) NOT NULL DEFAULT('ANY')
  );
END;


Seed at least one row for your test device (iPhone 13 example):

IF NOT EXISTS (SELECT 1 FROM dbo.DeviceModelCatalogs WHERE Brand='Apple' AND Model='iphone 13' AND DeviceType='Smartphone')
BEGIN
  INSERT dbo.DeviceModelCatalogs (Brand, Model, DeviceType, ReleaseYear, StorageGb)
  VALUES ('Apple','iphone 13','Smartphone',2021,128);

  DECLARE @id int = SCOPE_IDENTITY();
  INSERT dbo.DeviceBasePrices (DeviceModelCatalogId, BasePrice) VALUES (@id, 12000.00);
END;

3) AzureVision provider sanity checks

In appsettings*.json add:

"AzureVision": {
  "Endpoint": "https://<your-region>.api.cognitive.microsoft.com/",
  "Key": "YOUR_KEY",
  "DefaultConfidenceCutoff": 0.6
}


In Program.cs, ensure options binding & DI:

builder.Services.Configure<AzureVisionOptions>(
    builder.Configuration.GetSection("AzureVision"));

builder.Services.AddHttpClient<AzureVisionAssessmentProvider>(); // used internally

var provider = builder.Configuration["TradeInSettings:AI:Provider"] ?? "AzureVision";
if (provider.Equals("AzureVision", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<IDeviceAssessmentProvider, AzureVisionAssessmentProvider>();

4) Trade-in Details page: visible progress + polling

If you haven’t added it yet, keep the customer informed while the worker runs:

Status endpoint (simple):

// TradeInStatusController
[HttpGet("/tradein/status/{id:int}")]
public async Task<IActionResult> Get(int id, [FromServices] ApplicationDbContext db)
{
    var t = await db.TradeIns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    if (t == null) return NotFound();
    return Ok(new { t.Status, t.AutoOfferAmount, t.AutoGrade, t.AiVendor, t.AiConfidence });
}


On Details page, poll every 2–3s and update UI until status is one of: AutoQuoted, ManualReview, or Rejected. Show “Analyzing photos…” while status is Queued / Processing.

5) Re-run the happy path

Start the site (confirm TradeInAssessmentWorker logs “started”).

Submit a trade-in with 1–3 photos.

Watch the Details page: it should show “Analyzing…” while the worker calls Azure Vision, then either an auto-offer appears or the page states “Sent for manual review”.

6) Clean-up from earlier TraeAI error (optional)

Even if you won’t use it now, prevent that invalid URI crash:

// In TraeAiAssessmentProvider ctor
_httpClient.BaseAddress = new Uri("https://your-traeapi.example/"); // absolute

If anything still stalls

Check logs for TradeInAssessmentWorker → you should see “Processing TradeIn X”, then AzureVisionAssessmentProvider messages, then PricingService Calculating quote….

If you see “Invalid object name 'DeviceModelCatalogs'” again, the SQL in step 2 didn’t run against the same DB your app is using—double-check appsettings connection string.

If Azure Vision returns 401/404, recheck the Endpoint (must be a full https URL) and Key.