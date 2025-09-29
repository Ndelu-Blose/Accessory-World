# Domain Invariants & State Machine

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Invariants
- One TradeIn → at most one CreditNote (enforced by UNIQUE FK).
- CreditNote belongs to a single Customer and **only that** customer may redeem it.
- CreditNote redeemable exactly once; partial redemption optional but default is single-use.
- Expired or Cancelled notes are never applicable.
- AmountRemaining >= 0 at all times.

## State: TradeIn
- Submitted → UnderReview → Approved → Credited (terminal)
- UnderReview → Rejected (terminal)

Guards:
- Approve requires ApprovedValue > 0.
- Issue Credit Note only from Approved and only once.

## State: CreditNote
- Active → Redeemed | Expired | Cancelled
- PartiallyRedeemed (optional) if you allow partials.

## Acceptance
- State transitions enforced in services with unit tests for illegal transitions.
