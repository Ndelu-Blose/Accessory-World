# 70 — Rollback & Recovery Paths



---


- If Payfast timeout: auto‑cancel after TTL → release reservations.
- Admin action: **Reconcile** page to compare Payfast vs local orders.
- Refunds: create `Payment` with negative amount and `Status=Refunded`.
