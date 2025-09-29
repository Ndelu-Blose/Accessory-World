# Test Plan & Test Data

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Unit
- Illegal state transitions throw DomainException.
- Concurrency: two applies → one 200, one 409.
- Expired note apply → 422.

## Integration
- Full flow: submit → approve → issue → apply → webhook success → redeemed.
- Failure path: apply → payment fail → unlock → re-apply works.

## E2E
- Cypress/Playwright: UI apply, pay (mock), see discount.

## Test Data
- Users: alice (customer), bob (customer), admin.
- Notes: Active (R1000), Expired, Redeemed.
