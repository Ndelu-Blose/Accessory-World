# 80 — Trae AI Copy‑Paste Prompts



---


> Copy one block at a time into Trae.

### Task 10 — Catalog
```
You are a senior ASP.NET Core engineer. Implement the catalog list/detail per `10_catalog.md`.
- Create ProductCardVm, list action, and views with Available.
- Add index on Product(IsActive, OnHand).
- Validate with the acceptance criteria at the end of the file.
Return the exact files changed with unified diffs.
```

### Task 20 — Cart
```
Implement server-side cart with UnitPriceSnapshot per `20_cart.md`.
Ensure totals use snapshots only and qty capped by Available.
Add unit tests for snapshot pricing.
```

### Task 30 — Checkout
```
Implement atomic checkout per `30_checkout.md` using EF ExecutionStrategy.
Create Order/OrderLines with snapshots and reserve stock.
No manual transactions; include StockMovement logging.
Return orchestrator service + controller endpoint.
```

### Task 31 — Address
```
Wire address page & service per `31_address_validation.md`.
No manual transactions. Reload entity to get PublicId.
```

### Task 32 — Payfast
```
Build Payfast redirect & signature per `32_payment_payfast.md`.
Persist PaymentInit linking OrderId and m_payment_id.
Return HTTP 303 to Payfast.
```

### Task 34 — ITN + Idempotency
```
Create ITN endpoint per `34_webhooks_idempotency.md`.
Verify IP & signature. Enforce unique IdempotencyKey.
On COMPLETE → call workflow to mark Paid and deduct stock.
Return 200 on duplicates without reapplying.
```

### Task 40 — Tests
```
Add tests outlined in `40_tests.md`, especially idempotency and atomicity.
Provide test data builders and in-memory DB harness.
```
