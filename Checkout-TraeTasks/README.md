# Accessory‑World — Customer Journey Pack (Catalog → Cart → Checkout)

**Version:** v1 • **Date:** 2025-09-27

This pack is designed to be pasted **task‑by‑task** into Trae AI or executed manually.
Each task file contains: **Goal → Inputs → Steps → Code Changes → Acceptance Criteria → Validation → Trae Prompt**.

**Tech baseline (assumed):**
- ASP.NET Core 8 MVC + EF Core 8 + SQL Server
- Payfast (redirect + ITN) — tokenized, no raw card storage
- Existing AddressService fixed to avoid manual transactions
- Inventory tracked with `OnHand`, `Reserved`, `Available`

## Folder Map
- `00_overview.md` — end‑to‑end journey & data contracts
- `10_catalog.md` — product listing/view with stock indicators
- `20_cart.md` — add/remove/update with price snapshots
- `30_checkout.md` — orchestrator & order creation (atomic)
- `31_address_validation.md` — address UX + service wiring
- `32_payment_payfast.md` — redirect build, security & return URLs
- `33_order_workflow.md` — status transitions & stock reservations
- `34_webhooks_idempotency.md` — ITN handler with idempotency keys
- `40_tests.md` — unit/integration test plan + sample tests
- `50_migrations.md` — EF entities, indices, RowVersion fields
- `60_logging_observability.md` — structured logs + traces
- `70_rollback_recovery.md` — failure paths & compensations
- `80_trae_prompts.md` — copy‑paste prompts for Trae AI
- `95_env.sample` — required environment variables
- `postman/AccessoryWorld.postman_collection.json` — API checks

> Tip: Execute tasks in numeric order. If you already have parts, still run the **Acceptance Criteria** checks to confirm stability.
