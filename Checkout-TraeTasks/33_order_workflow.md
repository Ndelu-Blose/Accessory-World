# 33 — Order Workflow & Stock Deduction



---


**Goal:** Move `PendingPayment → Paid` on verified ITN, deduct stock, and trigger fulfilment.

### Steps
- Only **ITN handler** may move to `Paid`.
- On `Paid`:
  - For each line: `OnHand -= Qty`, `Reserved -= Qty`; log `Deduct`.
  - Create `Payment` row; mark `Order.Status = Paid`.
- On payment failure/cancel/timeout:
  - `Reserved` is **released**; log `Release` movements; `Order.Status = Cancelled`.

### Acceptance Criteria
- No double deduction on repeated ITNs (covered by idempotency task).
