# Payments Webhook & Idempotency

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Ensure pay-in webhooks are idempotent and finalize credit note redemption once, exactly once.

## Steps
1. Persist incoming webhook `eventId` with status (Processed/Failed) in `WebhookEvents` table.
2. If eventId already processed → return 200 immediately.
3. Within tx: mark event processed, finalize order, redeem note (if locked to this order).

## Acceptance
- Replaying the same webhook leaves system unchanged (idempotent).
