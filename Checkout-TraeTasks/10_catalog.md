# 10 — Catalog (List & Detail)



---


**Goal:** Reliable listing & detail views with available stock and add‑to‑cart entry points.

### Inputs
- Existing `Product` entity with `OnHand`, `Reserved`, `Price`.

### Steps
1. Add computed **Available** on the view model: `Available = OnHand - Reserved`.
2. Index on `IsActive, OnHand` for list queries.
3. Detail page renders qty selector bounded by `Available`.

### Code Changes (snippets)
**ViewModel**:
```csharp
public sealed class ProductCardVm 
{ 
    public Guid PublicId { get; set; } 
    public string Name { get; set; } 
    public string Sku { get; set; } 
    public decimal Price { get; set; } 
    public int Available { get; set; } 
}
```
**Controller (List)**:
```csharp
var items = await _db.Products.Where(p=>p.IsActive)
  .Select(p => new ProductCardVm 
  { 
      PublicId = p.PublicId, 
      Name = p.Name, 
      Sku = p.Sku, 
      Price = p.Price, 
      Available = p.OnHand - p.Reserved 
  })
  .ToListAsync();
```

### Acceptance Criteria
- List loads under 200ms for 100 products (cold < 500ms).
- Detail page caps qty to `Available` and disables button at `0`.

### Validation
- Simulate a product with `OnHand=5, Reserved=3` → `Available=2` shown.
