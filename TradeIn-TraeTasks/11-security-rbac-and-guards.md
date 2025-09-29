# Security: RBAC & Guards

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Guards
- Customer can only view/apply their own notes.
- Admin endpoints protected by role policies.
- Validate image URLs on upload to prevent SSRF; prefer signed blob storage URLs.

## Acceptance
- Attempt to use another user's note returns 403 with `code: not_owner`.
