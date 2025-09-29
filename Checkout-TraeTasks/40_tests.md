# 40 — Tests (Unit, Integration, E2E)



---


**Goal:** Guard rails for regressions on checkout.

### Coverage
- Cart math (snapshot pricing).
- Checkout atomicity (partial failure → rollback).
- ITN idempotency (duplicate notifications do not double‑apply).
- Stock transitions (Reserve → Deduct/Release).

### Sample (xUnit) — Idempotency
```csharp
[Fact]
public async Task Duplicate_ITN_Is_Ignored()
{
   // arrange: create order PendingPayment; send ITN payload twice
   // assert: only one Payment row; stock deducted once; status Paid
}
```
