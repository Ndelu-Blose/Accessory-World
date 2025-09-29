# 20 — Cart (Add/Update/Remove + Price Snapshot)



---


**Goal:** Cart stores a **UnitPriceSnapshot** per line; totals are deterministic.

### Steps
1) `POST /cart/add` validates `Available` and clamps qty.
2) When inserting a CartItem, **copy Product.Price → UnitPriceSnapshot**.
3) `POST /cart/update` re‑checks `Available` for the delta; adjust or reject.
4) Totals = `sum(UnitPriceSnapshot * Qty)` — no live price reads.

### Code Changes (snippets)
```csharp
public sealed class CartItem
{ 
    public int ProductId { get; set; } 
    public int Qty { get; set; } 
    public decimal UnitPriceSnapshot { get; set; } 
    public DateTime AddedAt { get; set; } 
}
```
**Service.AddToCart**:
```csharp
var p = await _db.Products.SingleAsync(x=>x.PublicId==pid);
var available = p.OnHand - p.Reserved;
var qty = Math.Min(request.Qty, available);
if (qty <= 0) return AddToCartResult.OutOfStock;
_cart.AddOrUpdate(pid, qty, unitPriceSnapshot: p.Price);
```

### Acceptance Criteria
- Cart totals remain unchanged even if `Product.Price` changes later.
- Attempting to add beyond `Available` caps and shows a message.
