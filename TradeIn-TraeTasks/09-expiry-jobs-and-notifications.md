# Expiry Jobs & Notifications

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Expire notes nightly and notify customers (30/7/1 days before).

## Steps
- Hangfire/Quartz job daily @02:00 UTC:
  - Find Active notes where `ExpiresAt < UtcNow` → set `Expired`.
- Notification job:
  - Send email/SMS reminders at D-30, D-7, D-1.

## Acceptance
- Expired notes cannot be applied; attempts return 422 with `code: expired_note`.
