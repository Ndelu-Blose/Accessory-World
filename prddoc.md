Accessory World — Product Requirements Document (PRD)
Last updated: 2025-08-25
0) Executive Snapshot
Vision: A reliable SA-based e‑commerce platform for mobile accessories and device trade‑ins with delivery and click‑and‑collect.
Primary outcomes (v1):
⦁	Checkout conversion ≥ 2.5%
⦁	Trade‑in acceptance rate ≥ 40%
⦁	Refund SLA ≤ 3 business days
⦁	Delivery On‑Time Rate ≥ 95%
Target market: South Africa (ZAR), English (v1), VAT registered.
Scope Boundary (v1)
In: Catalog, cart, checkout, Payfast card/EFT, delivery & pickup, trade‑in with credit note, refunds, inventory & stock ledger, order tracking, SMS/email comms, admin console, RBAC.
Later: Wallet/balance, loyalty, advanced promos, multi‑language, marketplace sellers, gift cards, BNPL.
1) Personas
⦁	Online Buyer: wants fast, trustworthy checkout and tracking.
⦁	Trade‑In Customer: wants clear valuation and instant credit.
⦁	Admin/Owner: configure catalog, prices, orders, refunds.
⦁	Inventory Manager: stock, movements, cycle counts.
⦁	Fulfilment Agent / Cashier: pick/pack; or OTP handover.
2) User Journeys (high level)
⦁	Browse → Cart → Checkout → Payment → Delivery/Pickup → Post‑purchase
⦁	Trade‑in submission → Evaluation → Offer → Credit note → Redemption
3) Business Rules (v1)
⦁	Credit notes are single‑use, customer‑bound, cannot exceed order total.
⦁	Pickup requires OTP (and ID for high‑value orders).
⦁	Address validation via geocoder; courier ETA & fee displayed pre‑payment.
⦁	Refunds to original tender; RMA required for item returns.
⦁	Stock cannot go negative (unless back‑order feature toggled on in future).
4) Functional Highlights (must haves)
⦁	Payments: Payfast (card + EFT) via server‑to‑server webhooks; idempotent handling.
⦁	Fulfilment: label creation via courier API; tracking sync.
⦁	Inventory: immutable stock movement ledger, reason codes.
⦁	Trade‑in: IMEI/serial validation, grade matrix, offer with expiry, credit note issue.
⦁	Notifications: Email + SMS templates for order status, OTP, trade‑in decisions.
5) Non‑Functional Requirements
⦁	p95 page TTFB ≤ 800ms; checkout page render ≤ 3s.
⦁	Webhook processing ≤ 1s; retries exponential backoff up to 24h.
⦁	Availability ≥ 99.9%; blue/green deploys.
⦁	POPIA & PCI: PII encryption at rest; never store raw PAN; signed webhooks.
⦁	Observability: request logs, traces, error rate alarms; audit trails for payments, refunds, stock.
6) Success Metrics & Analytics (top 10)
⦁	add_to_cart, checkout_start, payment_attempted, payment_succeeded, delivery_selected, pickup_selected, tradein_submitted, tradein_offer_accepted, creditnote_redeemed, refund_completed.
7) Acceptance Criteria (samples)
⦁	When a valid, unused credit note is applied by its owner, the order total is reduced and the note is marked Consumed only after successful payment.
⦁	Duplicate payment webhooks do not change the order state more than once.
⦁	OTP validation is single‑use and time‑boxed; invalid or expired OTPs are rejected.