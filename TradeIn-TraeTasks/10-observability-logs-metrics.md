# Observability: Logs & Metrics

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Structured Logs
- `tradein.submit`, `tradein.approve`, `creditnote.issue`, `creditnote.apply`, `creditnote.unlock`, `webhook.processed`.

## Metrics
- tradein_approved_total, creditnote_issued_total, creditnote_redeemed_total,
  creditnote_apply_conflicts_total, creditnote_expired_total.

## Acceptance
- Dashboards show conversion from trade-in → redeemed.
