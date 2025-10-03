# 5) Background worker — robust flow

Update your `TradeInAssessmentWorker` to **never leave the user in limbo**:

- On dequeue: set `Status = AiAnalysisInProgress`
- If provider throws or cannot grade: set `Status = ManualReview` and **still compute a conservative quote** if possible (or 0 + message)
- On success: set `Status = AiAnalysisCompleted` and store `AiAssessmentJson`, `AiVendor`, `AiVersion`, `AiConfidence`, `AutoGrade`, `AutoOfferAmount`, `AutoOfferBreakdownJson`

You likely already have most of this. Ensure **try/catch** around provider + pricing:

```csharp
try
{
    var assessment = await _provider.AnalyzeAsync(request, ct);
    var quote = await _pricing.QuoteAsync(tradeInId, assessment, ct);
    // persist fields, set Status = AiAnalysisCompleted
}
catch (Exception ex)
{
    _logger.LogError(ex, "AI processing failed for TradeIn {Id}", tradeInId);
    // mark ManualReview; optionally produce fallback quote
}
```

Also add small **retry** (e.g., once) if Azure transient failure occurs.
