# 00 — Overview & Contracts



---


## Customer Journey (Happy Path)
1) **Browse Catalog** → view product cards with **Available** stock badge.
2) **Product Detail** → choose variant/qty → **Add to Cart**.
3) **Cart** → modify qty → totals recompute (uses **price snapshot**).
4) **Checkout** → address, shipping, summary → **Place Order**:
   - Atomically create **Order** + **OrderLines** with **PriceSnapshot**
   - **Reserve** stock (not deduct OnHand yet), `Reserved += qty`
   - Redirect to **Payfast**
5) **Payfast Return** → thank‑you page (does NOT trust status).
6) **Payfast ITN (server‑to‑server)** → verify → mark **Paid** →
   - Convert **Reserved → OnHand deduction**, `OnHand -= qty`, `Reserved -= qty`
   - Create **Payment** + **StockMovement** logs
   - Trigger **Fulfilment**

## Key Data Contracts
### Entity: Product
- `Id (int)` PK
- `PublicId (uniqueidentifier)` — GUID for public use
- `Name`, `Sku`, `Price` (decimal(18,2)), `OnHand`, `Reserved`, `ReorderLevel`
- `IsActive (bit)`, `RowVersion (rowversion)`

### Entity: Cart & CartItem (server‑side OR cookie backed)
- `Cart.PublicId (guid)`, `UserId?`, `ExpiresAt`
- `CartItem.ProductId`, `Qty`, **`UnitPriceSnapshot`**

### Entity: Order
- `Id`, `PublicId (guid UNIQUE)`
- `UserId?`, `Email`, `CreatedAt`
- `Status` (enum): `PendingPayment`, `Paid`, `Cancelled`, `Refunded`, `Fulfilment`, `Completed`
- **`TotalSnapshot`** (decimal), **`Currency`**
- `ShippingAddressId (FK)`
- `PaymentRef` (string, nullable until set)
- `RowVersion (rowversion)`

### Entity: OrderLine
- `OrderId`, `ProductId`, `Qty`
- **`UnitPriceSnapshot`**, **`LineTotalSnapshot`**
- `RowVersion (rowversion)`

### Entity: Payment
- `Id`, `Gateway` = "Payfast", `GatewayTxnId`, `Amount`, `Currency`, `Status`, `RawPayload (json)`, `CreatedAt`
- **`IdempotencyKey`** (unique) — e.g., Payfast `pf_payment_id` or computed signature

### Entity: StockMovement
- `Id`, `ProductId`, `Type` (`Reserve`, `Release`, `Deduct`, `Restock`), `Qty`, `OrderId?`, `Reason`, `CreatedAt`

## Invariants
- **Price snapshots are immutable** on Order and Lines.
- **Stock gating**: `Available = OnHand - Reserved`; cart add validates against `Available`.
- **Checkout is atomic** via EF Core **ExecutionStrategy** (NO manual transactions).
- **ITN is the source of truth**; return page is informational.
