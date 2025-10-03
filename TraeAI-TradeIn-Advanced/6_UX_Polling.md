# 6) UX polling — controller + JS (no more stuck spinner)

## Controller endpoint
Add a lightweight status endpoint the Details page can poll:

See `/CodeSnippets/TradeInStatusControllerSnippet.cs`. It returns JSON:
```json
{ "status": "AiAnalysisInProgress", "hasOffer": true, "offer": 8450.00 }
```

## Details.cshtml
Inject a **poller** that calls the endpoint every few seconds and updates the UI.

See `/CodeSnippets/TradeInDetailsPollingSnippet.cshtml` for a ready‑to‑paste `<script>` block:
- shows “Analyzing…” while `AiAnalysisInProgress`
- shows final offer once `AiAnalysisCompleted`
- if `ManualReview`, shows message and stops polling
- handles network errors gracefully
