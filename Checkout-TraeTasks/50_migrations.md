# 50 — Migrations & Indexing



---


**Goal:** Add/ensure columns and indices for concurrency & speed.

### EF Annotations
- `RowVersion` → `[Timestamp]` byte[] for Product, Order, OrderLine.
- Unique index on `Order.PublicId` and `Payment.IdempotencyKey`.
- Index on `Product(IsActive, OnHand)` and `StockMovement(ProductId, CreatedAt)`.
