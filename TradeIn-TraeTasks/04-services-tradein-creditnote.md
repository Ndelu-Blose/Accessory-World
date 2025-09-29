# Services: TradeIn & CreditNote

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Implement application services with transactional consistency and concurrency protection.

## Key Functions (signatures)
- `TradeInService.SubmitAsync(TradeInDto dto, Guid customerId)`
- `TradeInService.MoveToReviewAsync(Guid tradeInId, Guid adminId)`
- `TradeInService.ApproveAsync(Guid tradeInId, decimal value, Guid adminId)`
- `TradeInService.RejectAsync(Guid tradeInId, string reason, Guid adminId)`
- `TradeInService.IssueCreditNoteAsync(Guid tradeInId, DateTime expiresAt)`
- `CreditNoteService.ApplyAsync(string code, Guid customerId, Guid orderId, decimal orderTotal, string idemKey)`
- `CreditNoteService.UnlockAsync(Guid creditNoteId, string reason)`

## Concurrency & Idempotency
- `RowVersion` checks on updates; catch `DbUpdateConcurrencyException` and retry (max 3).
- `idemKey` unique per (customerId, orderId, code) to guard double-apply.
- Use SERIALIZABLE tx or `SELECT ... WITH (UPDLOCK, ROWLOCK)` when decrementing remaining value.

## Pseudocode: Apply
```
Begin Tx
  note = GetByCode(code) FOR UPDATE
  if note.CustomerId != customerId or note.Status != Active or UtcNow > ExpiresAt: fail
  if note.LockedByOrderId != null and != orderId: fail (already locked)
  Lock note to orderId; Save
Commit

// Later on payment webhook SUCCESS:
Begin Tx
  note = GetByCode(code) FOR UPDATE
  if note.LockedByOrderId == orderId:
     note.Status = Redeemed
     note.RedeemedAt = UtcNow
     note.AmountRemaining = 0
     Save
Commit

// On payment FAILED/TIMED OUT:
UnlockAsync(noteId)
```
## Acceptance
- Race between two carts applying same code → exactly one wins; other sees 409.
