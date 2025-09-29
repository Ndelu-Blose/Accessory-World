# 34 — ITN Webhook + Idempotency (Source of Truth)



---


**Goal:** Secure ITN endpoint that is **idempotent** and verifies origin.

### Steps
1) Verify source IP (Payfast published ranges) **and** signature.
2) Look up `m_payment_id` → `OrderId`. Compute **IdempotencyKey** from `pf_payment_id` (or hash of fields).
3) **Begin ExecutionStrategy block**; insert `Payment` **only if** IdempotencyKey not seen (unique index).
4) Map Payfast status → internal: `COMPLETE → Paid`, else `Cancelled/Failed`.
5) Apply workflow transitions from task 33.
6) Record full raw payload in `Payment.RawPayload` (for audits).

### Acceptance Criteria
- Replaying the same ITN does **nothing** (returns 200, no changes).
- Invalid signature/IP returns 400 and logs a warning (no state change).
