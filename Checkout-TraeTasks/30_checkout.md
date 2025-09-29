# 30 — Checkout Orchestrator (Atomic Order Creation)



---


**Goal:** One call to create **Order + OrderLines + Reserve stock** atomically, then redirect to Payfast.

### Inputs
- Validated `ShippingAddressId` (from 31 task).
- Current `Cart` items with `UnitPriceSnapshot` and `Qty`.

### Steps
- Use EF Core **ExecutionStrategy**; **do not** open manual transactions.
- For each cart line:
  - Re‑read Product row and check concurrency via `RowVersion` when saving.
  - Validate `Available >= Qty`.
  - Increment `Reserved += Qty` and log `StockMovement(Reserve)`.
- Insert `Order` + `OrderLines` from **snapshots** and compute `TotalSnapshot`.
- Save changes; return `Order.PublicId` and a `PaymentInit` DTO (for task 32).

### Code (core pattern)
```csharp
await _executionStrategy.ExecuteAsync(async () =>
{
    foreach (var line in cart.Items)
    {
        var p = await _db.Products.SingleAsync(x=>x.Id==line.ProductId);
        var available = p.OnHand - p.Reserved;
        if (available < line.Qty) throw new DomainException("Insufficient stock");
        p.Reserved += line.Qty;
        _db.StockMovements.Add(new StockMovement
        { 
            ProductId = p.Id, 
            Type = MovementType.Reserve, 
            Qty = line.Qty, 
            OrderId = order.Id, 
            Reason = "Checkout" 
        });
    }
    _db.Orders.Add(order);
    await _db.SaveChangesAsync();
});
```

### Acceptance Criteria
- If any line fails validation, **no** rows are persisted (all or nothing).
- Order ends in `PendingPayment` with correct totals and `Reserved` increments.
