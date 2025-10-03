Awesome—let’s wire in **Azure Computer Vision (free tier)** for your Trade-In AI. I’ll give you a **fool-proof, step-by-step** plan with ready-to-paste code and the safety nets so your UI never “hangs” again.

---

# 0) One-time Azure setup (free)

1. Go to Azure Portal → **Create resource** → search **“Azure AI services”** → choose **“Computer Vision”** (it’s inside Azure AI Services).
2. Pricing tier: **F0 (free)**.
3. After deploy, open the resource → copy:

   * **Endpoint** (e.g., `https://<your-region>.api.cognitive.microsoft.com/`)
   * **Key** (Key1)

> Keep these handy—we’ll put them in `appsettings.json`.

---

# 1) Install SDKs

In your web project:

```bash
dotnet add package Azure.AI.Vision.ImageAnalysis
dotnet add package Azure.Identity
```

> `Azure.AI.Vision.ImageAnalysis` is the **Image Analysis 4.0** SDK (the current one).

---

# 2) Configuration

`appsettings.json` (and `appsettings.Development.json`)

```json
{
  "AzureVision": {
    "Endpoint": "https://<your-region>.api.cognitive.microsoft.com/",
    "Key":      "<YOUR_KEY>",
    "TimeOutSeconds": 20
  },
  "AiAssessment": {
    "Provider": "AzureVision"   // <- switch from TraeAI to AzureVision
  }
}
```

---

# 3) DI wiring (Program.cs)

Add a typed options + provider:

```csharp
using Azure.AI.Vision.ImageAnalysis;
using Azure;

builder.Services.Configure<AzureVisionOptions>(
    builder.Configuration.GetSection("AzureVision"));

builder.Services.AddSingleton<ImageAnalysisClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AzureVisionOptions>>().Value;
    return new ImageAnalysisClient(
        new Uri(opts.Endpoint),
        new AzureKeyCredential(opts.Key),
        new ImageAnalysisClientOptions { }
    );
});

// Tell the app to use AzureVision for assessments
builder.Services.AddSingleton<IDeviceAssessmentProvider, AzureVisionAssessmentProvider>();
```

Create a tiny options class:

```csharp
public sealed class AzureVisionOptions
{
    public string Endpoint { get; set; } = "";
    public string Key { get; set; } = "";
    public int TimeOutSeconds { get; set; } = 20;
}
```

> If you still want a feature flag, you can resolve `IDeviceAssessmentProvider` conditionally based on `AiAssessment:Provider`.

---

# 4) Implement the provider

Create `Services/AI/AzureVisionAssessmentProvider.cs`

```csharp
using Azure;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AccessoryWorld.Services.AI
{
    public sealed class AzureVisionAssessmentProvider : IDeviceAssessmentProvider
    {
        private readonly ImageAnalysisClient _client;
        private readonly AzureVisionOptions _opts;
        private static readonly string[] DamageKeywords =
            { "crack", "cracked", "broken", "shatter", "scratch", "scratched", "dent", "scuff", "chip" };

        public AzureVisionAssessmentProvider(
            ImageAnalysisClient client,
            IOptions<AzureVisionOptions> opts)
        {
            _client = client;
            _opts = opts.Value;
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(DeviceAssessmentRequest request, CancellationToken ct)
        {
            var perImageFindings = new List<ImageFindings>();

            foreach (var photoPath in request.Photos ?? Enumerable.Empty<string>())
            {
                // Resolve absolute path on disk
                var absPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(absPath)) continue;

                using var fs = File.OpenRead(absPath);

                // Ask Azure for: caption, tags, objects, and text
                var features =
                    VisualFeatures.Caption
                    | VisualFeatures.Tags
                    | VisualFeatures.Objects
                    | VisualFeatures.Read;

                var result = await _client.AnalyzeAsync(BinaryData.FromStream(fs), features, ct);

                var tags = result.Tags?.Select(t => new TagScore(t.Name, t.Confidence))?.ToList() ?? new();
                var objectsFound = result.Objects?.Select(o => new TagScore(o.Name, o.Confidence))?.ToList() ?? new();
                var caption = result.Caption?.Text ?? string.Empty;
                var text = string.Join(" ", result.Read?.Blocks?.SelectMany(b => b.Lines).Select(l => l.Text) ?? Array.Empty<string>());

                // Simple damage scoring: look in caption + tags + detected objects + OCR text
                var blob = (caption + " " + text + " " + string.Join(" ", tags.Select(t => t.Name)) + " " + string.Join(" ", objectsFound.Select(o => o.Name)))
                           .ToLowerInvariant();

                int damageHits = DamageKeywords.Count(k => blob.Contains(k));

                perImageFindings.Add(new ImageFindings
                {
                    PhotoPath = photoPath,
                    Caption = caption,
                    Tags = tags,
                    Objects = objectsFound,
                    Text = text,
                    DamageHits = damageHits
                });
            }

            // Aggregate a confidence/grade (very conservative baseline)
            var totalHits = perImageFindings.Sum(f => f.DamageHits);
            var hasAnyCrack = perImageFindings.Any(f => f.BlobContains("crack") || f.BlobContains("broken") || f.BlobContains("shatter"));
            var hasManyScratches = perImageFindings.Sum(f => f.BlobCount("scratch")) >= 3;

            string suggestedGrade =
                hasAnyCrack ? "D" :
                hasManyScratches ? "C" :
                totalHits >= 1 ? "B" : "A";

            // Confidence heuristic (0..1)
            double confidence =
                perImageFindings.Count == 0 ? 0.0 :
                Math.Min(1.0, 0.35 + 0.15 * totalHits);

            // Build a JSON payload you can store
            var payload = new
            {
                photos = perImageFindings.Select(f => new {
                    f.PhotoPath,
                    f.Caption,
                    tags = f.Tags.Select(t => new { t.Name, t.Confidence }),
                    objectsFound = f.Objects.Select(o => new { o.Name, o.Confidence }),
                    text = f.Text,
                    f.DamageHits
                }),
                suggestedGrade,
                confidence
            };

            return new DeviceAssessmentResult
            {
                Vendor = "AzureVision",
                Version = "4.0",
                Confidence = confidence,
                SuggestedGrade = suggestedGrade,
                AssessmentJson = JsonSerializer.Serialize(payload)
            };
        }

        private sealed record TagScore(string Name, double Confidence);

        private sealed class ImageFindings
        {
            public string PhotoPath { get; set; } = "";
            public string Caption { get; set; } = "";
            public List<TagScore> Tags { get; set; } = new();
            public List<TagScore> Objects { get; set; } = new();
            public string Text { get; set; } = "";
            public int DamageHits { get; set; }

            public bool BlobContains(string key)
                => (Caption + " " + Text + " " + string.Join(" ", Tags.Select(t => t.Name)) + " " + string.Join(" ", Objects.Select(o => o.Name)))
                    .ToLowerInvariant().Contains(key);

            public int BlobCount(string key)
            {
                var all = (Caption + " " + Text + " " + string.Join(" ", Tags.Select(t => t.Name)) + " " + string.Join(" ", Objects.Select(o => o.Name)))
                          .ToLowerInvariant();
                int count = 0, idx = 0;
                while ((idx = all.IndexOf(key, idx, StringComparison.Ordinal)) >= 0) { count++; idx += key.Length; }
                return count;
            }
        }
    }
}
```

> This gives you a **clean, deterministic** result: `SuggestedGrade`, `Confidence`, and a full `AssessmentJson` you can show/admin-review.

---

# 5) Use it in your worker

Your existing `TradeInAssessmentWorker` probably calls `IDeviceAssessmentProvider`. Keep that. Just ensure you handle **failures gracefully**:

```csharp
try
{
    var assessment = await _provider.AnalyzeAsync(req, ct);

    tradeIn.AiVendor = assessment.Vendor;
    tradeIn.AiVersion = assessment.Version;
    tradeIn.AiConfidence = assessment.Confidence;
    tradeIn.AutoGrade = assessment.SuggestedGrade;
    tradeIn.AiAssessmentJson = assessment.AssessmentJson;

    var quote = await _pricing.QuoteAsync(tradeIn.Id, assessment, ct);
    tradeIn.AutoOfferAmount = quote.Amount;
    tradeIn.AutoOfferBreakdownJson = quote.BreakdownJson;

    tradeIn.Status = TradeInStatus.AiAnalysisCompleted; // your enum
}
catch (Exception ex)
{
    _logger.LogError(ex, "Azure Vision analysis failed for TradeIn {Id}", tradeIn.Id);

    // IMPORTANT: never leave the UI “spinning”
    tradeIn.Status = TradeInStatus.ManualReview; // fall back
    tradeIn.Notes = (tradeIn.Notes ?? "") + "\nAI analysis failed; routed to manual review.";
}

await _db.SaveChangesAsync(ct);
```

---

# 6) Fix your **pricing catalog** error (blocking!)

Your logs show `Invalid object name 'DeviceModelCatalogs'`. Create the tables if they’re missing (quick SQL):

```sql
IF OBJECT_ID('dbo.DeviceModelCatalogs','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceModelCatalogs(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Brand NVARCHAR(64) NOT NULL,
    Model NVARCHAR(128) NOT NULL,
    DeviceType NVARCHAR(32) NOT NULL,
    ReleaseYear INT NOT NULL,
    StorageGb INT NULL
  );
  CREATE UNIQUE INDEX IX_DeviceModelCatalogs_Brand_Model_Type
    ON dbo.DeviceModelCatalogs(Brand, Model, DeviceType);
END;

IF OBJECT_ID('dbo.DeviceBasePrices','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceBasePrices(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    DeviceModelCatalogId INT NOT NULL
      REFERENCES dbo.DeviceModelCatalogs(Id) ON DELETE CASCADE,
    BasePrice DECIMAL(18,2) NOT NULL,
    AsOf DATETIME2 NOT NULL DEFAULT(sysutcdatetime())
  );
END;

IF OBJECT_ID('dbo.PriceAdjustmentRules','U') IS NULL
BEGIN
  CREATE TABLE dbo.PriceAdjustmentRules(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(64) NOT NULL,
    Multiplier DECIMAL(9,4) NOT NULL,
    FlatDeduction DECIMAL(18,2) NULL,
    AppliesTo NVARCHAR(32) NOT NULL DEFAULT('ANY')
  );
END;
```

Seed at least one device so the quote doesn’t fail:

```sql
IF NOT EXISTS (SELECT 1 FROM dbo.DeviceModelCatalogs WHERE Brand='Apple' AND Model='iphone 13' AND DeviceType='Smartphone')
BEGIN
  INSERT dbo.DeviceModelCatalogs(Brand,Model,DeviceType,ReleaseYear,StorageGb)
  VALUES('Apple','iphone 13','Smartphone',2021,128);

  DECLARE @id INT = SCOPE_IDENTITY();
  INSERT dbo.DeviceBasePrices(DeviceModelCatalogId, BasePrice) VALUES(@id, 12000); -- example
END;
```

---

# 7) Make the **UI fool-proof**

You noticed the spinner disappeared and users got confused. Fix the flow:

**a) Status machine (server):**

* When user submits: set `Status = AiAnalysisQueued`.
* When worker starts: set `Status = AiAnalysisInProgress`.
* On success: `AiAnalysisCompleted` (and set `AutoOfferAmount`).
* On any exception: **set `Status = ManualReview`** (never leave in “InProgress”).

**b) Details page (client):**

* Poll `/TradeIn/Status?id=<publicId>` every 4–6s (or use SignalR if you prefer).
* Show:

  * “AI analysis in progress…” only if status is `AiAnalysisInProgress`.
  * “Offer ready” if `AiAnalysisCompleted`.
  * **If `ManualReview`: show a clear banner** → “We couldn’t finish automated analysis; a specialist will review this and send an offer. Expect within 1 business day.”

This guarantees users aren’t stuck staring at a spinner.

---

# 8) Test checklist

1. Submit a trade-in with 1–3 images (heavy images OK; SDK handles streams).
2. Worker logs show “Processing TradeIn X”.
3. No more `Invalid object name` errors from pricing.
4. Status transitions make sense and **UI reflects them**.
5. Try disconnecting your internet to force Azure error → verify status goes to **ManualReview** and the banner shows.

---

## Notes & Tips

* **Timeouts**: The SDK already retries; you can add a `CancellationToken` with 20–30s timeout per image.
* **Cost control**: Free tier 5k images/month → you’re safe for dev.
* **Accuracy**: This baseline uses tags/caption/text. If you need stronger “crack detection,” add a tiny custom model later (Roboflow) and run both; if custom says “crack,” override to C/D.
* **Security**: Never log the raw key. Use Key Vault for prod.





Perfect — here’s a **drop-in test stub** for `IDeviceAssessmentProvider` that  Azure’s results so you can test the full flow (queue → worker → pricing → UI) even if the Azure resource isn’t ready or you’re offline.

It’s deterministic, fast, and you can **force scenarios** (success, fail, specific grade) from the Trade-In description so QA is easy.

---

# 1) Config switch

In `appsettings.Development.json` (or `appsettings.json` while testing):

```json
{
  "AiAssessment": {
    "Provider": "Stub"      // "AzureVision" for real, "Stub" for fake
  },
  "AiAssessmentStub": {
    "MinDelayMs": 800,      // simulate processing time (range)
    "MaxDelayMs": 1800,
    "DefaultGrade": "B",
    "DefaultConfidence": 0.72,
    "RandomizeSlightly": true,
    "FailureRatePercent": 0  // set 5 or 10 to test fallback → ManualReview
  }
}
```

---

# 2) Wire up DI (Program.cs)

```csharp
// after builder.Configuration is available
var providerName = builder.Configuration["AiAssessment:Provider"] ?? "AzureVision";
if (string.Equals(providerName, "Stub", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.Configure<AiAssessmentStubOptions>(
        builder.Configuration.GetSection("AiAssessmentStub"));
    builder.Services.AddSingleton<IDeviceAssessmentProvider, StubAssessmentProvider>();
}
else
{
    // your AzureVision wiring from earlier
    builder.Services.Configure<AzureVisionOptions>(
        builder.Configuration.GetSection("AzureVision"));
    builder.Services.AddSingleton<ImageAnalysisClient>(sp =>
    {
        var opts = sp.GetRequiredService<IOptions<AzureVisionOptions>>().Value;
        return new ImageAnalysisClient(new Uri(opts.Endpoint), new AzureKeyCredential(opts.Key));
    });
    builder.Services.AddSingleton<IDeviceAssessmentProvider, AzureVisionAssessmentProvider>();
}
```

---

# 3) The stub provider (drop-in)

Create `Services/AI/StubAssessmentProvider.cs`

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AccessoryWorld.Services.AI
{
    public sealed class AiAssessmentStubOptions
    {
        public int MinDelayMs { get; set; } = 800;
        public int MaxDelayMs { get; set; } = 1800;
        public string DefaultGrade { get; set; } = "B";
        public double DefaultConfidence { get; set; } = 0.72;
        public bool RandomizeSlightly { get; set; } = true;
        public int FailureRatePercent { get; set; } = 0; // 0..100
    }

    /// <summary>
    /// A deterministic fake of IDeviceAssessmentProvider that:
    /// - delays a bit,
    /// - returns a grade/confidence based on description + filenames,
    /// - supports forced outcomes using control tokens in Description.
    /// 
    /// Control tokens (put in Description):
    ///   FORCE_FAIL            → throws to test ManualReview fallback
    ///   FORCE_GRADE:A|B|C|D  → returns exact grade
    ///   FORCE_CONF:<0..1>    → returns exact confidence (e.g., FORCE_CONF:0.35)
    /// </summary>
    public sealed class StubAssessmentProvider : IDeviceAssessmentProvider
    {
        private readonly AiAssessmentStubOptions _opts;
        private readonly Random _random = new Random();

        public StubAssessmentProvider(IOptions<AiAssessmentStubOptions> opts)
        {
            _opts = opts.Value;
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(
            DeviceAssessmentRequest request,
            CancellationToken ct)
        {
            // 1) Simulate processing latency
            int delay = _random.Next(_opts.MinDelayMs, _opts.MaxDelayMs + 1);
            await Task.Delay(delay, ct);

            var desc = request.Description ?? string.Empty;

            // 2) Forced failure?
            if (desc.Contains("FORCE_FAIL", StringComparison.OrdinalIgnoreCase)
                || WillFailByRate())
            {
                throw new InvalidOperationException("Stubbed AI failure requested");
            }

            // 3) Decide grade
            string grade = ParseForcedGrade(desc) ?? InferGrade(request, desc, fallback: _opts.DefaultGrade);

            // 4) Decide confidence
            double confidence = ParseForcedConfidence(desc)
                                ?? (_opts.DefaultConfidence + (_opts.RandomizeSlightly ? Jitter(0.07) : 0));
            confidence = Math.Clamp(confidence, 0.05, 0.99);

            // 5) Build a transparent JSON payload (useful for UI/Admin)
            var payload = new
            {
                provider = "Stub",
                version = "1.0",
                decidedAt = DateTimeOffset.UtcNow,
                inputs = new
                {
                    request.DeviceBrand,
                    request.DeviceModel,
                    request.DeviceType,
                    request.Description,
                    photos = request.Photos ?? new List<string>()
                },
                findings = new
                {
                    keywords = ExtractKeywords(desc, request.Photos),
                    rules = new[]{
                        "crack/broken/shatter → D",
                        "scratch/scraped     → C",
                        "dent/scuff/chip     → C",
                        "no issues           → A/B"
                    }
                },
                suggestedGrade = grade,
                confidence
            };

            return new DeviceAssessmentResult
            {
                Vendor = "Stub",
                Version = "1.0",
                SuggestedGrade = grade,
                Confidence = confidence,
                AssessmentJson = JsonSerializer.Serialize(payload)
            };
        }

        private bool WillFailByRate()
        {
            if (_opts.FailureRatePercent <= 0) return false;
            return _random.Next(0, 100) < _opts.FailureRatePercent;
        }

        private static string? ParseForcedGrade(string text)
        {
            // e.g., FORCE_GRADE:C
            const string key = "FORCE_GRADE:";
            var idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var val = text.Substring(idx + key.Length).Trim().ToUpperInvariant();
            // take first char A/B/C/D
            if (val.Length > 0 && "ABCD".Contains(val[0])) return val[0].ToString();
            return null;
        }

        private static double? ParseForcedConfidence(string text)
        {
            // e.g., FORCE_CONF:0.35
            const string key = "FORCE_CONF:";
            var idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            var tail = text.Substring(idx + key.Length).Trim();
            if (double.TryParse(tail, out var conf))
                return conf;
            return null;
        }

        private string InferGrade(DeviceAssessmentRequest req, string description, string fallback)
        {
            string text = (description + " " + string.Join(" ", req.Photos ?? new List<string>())).ToLowerInvariant();

            bool hasCrack = text.Contains("crack") || text.Contains("cracked") || text.Contains("broken") || text.Contains("shatter");
            bool hasScratch = text.Contains("scratch") || text.Contains("scratched") || text.Contains("scrape");
            bool hasDent = text.Contains("dent") || text.Contains("scuff") || text.Contains("chip");

            if (hasCrack) return "D";
            if (hasScratch || hasDent) return "C";

            // make it deterministic but varied: hash brand+model
            int bucket = HashBucket($"{req.DeviceBrand}|{req.DeviceModel}", 100);
            return bucket < 30 ? "A" : "B";
        }

        private static IEnumerable<string> ExtractKeywords(string description, IEnumerable<string>? photos)
        {
            var words = (description ?? "").ToLowerInvariant().Split(new[] { ' ', ',', '.', ';', ':', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>(words);
            if (photos != null) list.AddRange(photos.Select(Path.GetFileName).Where(s => s != null)!);
            return list.Distinct().Take(20);
        }

        private static int HashBucket(string text, int modulo)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            int val = BitConverter.ToInt32(bytes, 0);
            if (val < 0) val = -val;
            return val % modulo;
        }

        private double Jitter(double amplitude)
        {
            // random value in [-amplitude, +amplitude]
            return (2 * _random.NextDouble() - 1) * amplitude;
        }
    }
}
```

> It honors your project’s `DeviceAssessmentRequest`/`DeviceAssessmentResult` types. If your property names differ, just map accordingly.

---

# 4) How to use it (QA scenarios)

On the **Submit Trade-In** form, put the following in **Description** to simulate flows:

* `FORCE_GRADE:A` → returns Grade A
* `FORCE_GRADE:C` → returns Grade C
* `FORCE_CONF:0.33` → sets confidence to 0.33
* `FORCE_FAIL` → provider throws → your worker should set status to **ManualReview** and the UI must show the manual-review banner (no endless spinner)

Or just write natural text like *“Cracked screen with scratches”* to get **D/C** automatically.

---

# 5) Safety net (so the UI never hangs)

You already log and transition statuses — keep this in the worker:

* Start: `AiAnalysisInProgress`
* On stub success: `AiAnalysisCompleted` (+ set `AutoGrade`, `AutoOfferAmount`)
* On exception (including `FORCE_FAIL`): **set `ManualReview`** and append reason to notes
* Details page: poll or SignalR; if `ManualReview`, show a clear banner

---

# 6) Switching to real Azure later

When your Azure Computer Vision key is ready, just change:

```json
"AiAssessment": { "Provider": "AzureVision" }
```

No code changes elsewhere; the DI will switch to `AzureVisionAssessmentProvider`.

---

