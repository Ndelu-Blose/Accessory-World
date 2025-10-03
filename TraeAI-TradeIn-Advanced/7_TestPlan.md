# 7) Test plan (fool‑proof)

1) **DB ready**
- Run the SQL from `1_DatabaseFix.md` (Path A). Verify tables exist and at least one iPhone 13 seed row.

2) **Azure ready**
- Create Azure Vision (free tier), copy Endpoint + Key into `appsettings.json` (section `AzureVision`).

3) **Build & run**
```
dotnet build
dotnet run
```

4) **Submit a Trade-In**
- Brand: Apple
- Model: iPhone 13
- Type: Smartphone
- Condition: “Cracked screen” + upload 1–2 photos
- Proposed Value: 9000

5) **Observe**
- Details page should show **“Analyzing…”** (spinner/polling).
- Background logs: provider is called; status flips to `AiAnalysisInProgress` → `AiAnalysisCompleted` (or `ManualReview` if Azure fails).

6) **Offer**
- Offer should appear when pricing completes. If catalog didn’t match, the UI should still show a graceful message and suggest manual review.

7) **Edge cases**
- Disconnect internet → expect `ManualReview` and no infinite spinner.
- Azure key wrong → provider throws; worker catches → `ManualReview`, UI shows message.
