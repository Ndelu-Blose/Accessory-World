# Runbook & Edge Cases

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Common Issues
- **409 conflict on apply**: Someone else applied first; advise retry after unlock or contact support.
- **Refund policy**: On refund, re-issue a new Credit Note with same value; never cash-out.
- **Partial orders**: If you later allow partial redemption, store `AmountRemaining` and an array of `RedemptionEvents`.

## Manual Ops
- Admin page: search code → Cancel or Extend expiry (with audit log).
