# Checkout Integration & Locking

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Apply and lock credit notes during checkout; finalize on payment success.

## Steps
1. Add "Apply Credit Note" box on Cart/Checkout.
2. On apply:
   - Call `/api/checkout/apply-credit-note` with Idempotency-Key.
   - Show discount; store lock token in session (orderId).
3. On payment success webhook → display success; clear session.
4. On failure → call `UnlockAsync` and remove discount.

## Acceptance
- UI prevents editing code after lock; shows expiry date and terms.
