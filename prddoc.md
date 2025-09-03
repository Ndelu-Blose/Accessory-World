Accessory World — Product Requirements Document (PRD)
Last updated: January 2025 - Updated with product card improvements and UI enhancements
0) Executive Snapshot
Vision: A reliable SA-based e‑commerce platform for mobile accessories and device trade‑ins with delivery and click‑and‑collect.
Primary outcomes (v1):
⦁	Checkout conversion ≥ 2.5%
⦁	Trade‑in acceptance rate ≥ 40%
⦁	Refund SLA ≤ 3 business days
⦁	Delivery On‑Time Rate ≥ 95%
⦁	Product browsing engagement ≥ 3 pages per session
⦁	Product card interaction rate ≥ 15%
Target market: South Africa (ZAR), English (v1), VAT registered.
Scope Boundary (v1)
In: Enhanced catalog with professional product cards, improved pricing display, responsive grid layout, cart, checkout, Payfast card/EFT, delivery & pickup, trade‑in with credit note, refunds, inventory & stock ledger, order tracking, SMS/email comms, admin console, RBAC.
Recently Added: Professional product card styling, proper price formatting (single/range), enhanced visual design with shadows and typography, responsive product grid system.
Later: Wallet/balance, loyalty, advanced promos, multi‑language, marketplace sellers, gift cards, BNPL.
1) Personas
⦁	Online Buyer: wants fast, trustworthy checkout and tracking.
⦁	Trade‑In Customer: wants clear valuation and instant credit.
⦁	Admin/Owner: configure catalog, prices, orders, refunds.
⦁	Inventory Manager: stock, movements, cycle counts.
⦁	Fulfilment Agent / Cashier: pick/pack; or OTP handover.
2) User Journeys (high level)
⦁	Browse (Enhanced Product Cards) → Filter/Search → Product Detail → Cart → Checkout → Payment → Delivery/Pickup → Post‑purchase
⦁	Trade‑in submission → Evaluation → Offer → Credit note → Redemption
⦁	Product Discovery: Landing page → Product categories → Enhanced product grid → Individual product cards with proper pricing
3) Business Rules (v1)
⦁	Credit notes are single‑use, customer‑bound, cannot exceed order total.
⦁	Pickup requires OTP (and ID for high‑value orders).
⦁	Address validation via geocoder; courier ETA & fee displayed pre‑payment.
⦁	Refunds to original tender; RMA required for item returns.
⦁	Stock cannot go negative (unless back‑order feature toggled on in future).
4) Functional Highlights (must haves)
⦁	Product Display: Professional product cards with enhanced styling, proper price formatting (single price vs. price ranges), responsive grid layout, improved typography and visual hierarchy.
⦁	Payments: Payfast (card + EFT) via server‑to‑server webhooks; idempotent handling.
⦁	Fulfilment: label creation via courier API; tracking sync.
⦁	Inventory: immutable stock movement ledger, reason codes.
⦁	Trade‑in: IMEI/serial validation, grade matrix, offer with expiry, credit note issue.
⦁	Notifications: Email + SMS templates for order status, OTP, trade‑in decisions.
⦁	UI/UX: Enhanced product browsing experience with professional card design, consistent spacing, modern shadows, and improved visual appeal.
5) Non‑Functional Requirements
⦁	p95 page TTFB ≤ 800ms; checkout page render ≤ 3s.
⦁	Webhook processing ≤ 1s; retries exponential backoff up to 24h.
⦁	Availability ≥ 99.9%; blue/green deploys.
⦁	POPIA & PCI: PII encryption at rest; never store raw PAN; signed webhooks.
⦁	Observability: request logs, traces, error rate alarms; audit trails for payments, refunds, stock.
6) Success Metrics & Analytics (top 12)
⦁	product_card_viewed, product_card_clicked, add_to_cart, checkout_start, payment_attempted, payment_succeeded, delivery_selected, pickup_selected, tradein_submitted, tradein_offer_accepted, creditnote_redeemed, refund_completed.
⦁	Product Engagement: card_hover_time, price_display_effectiveness, grid_scroll_depth.
7) Acceptance Criteria (samples)
⦁	When a valid, unused credit note is applied by its owner, the order total is reduced and the note is marked Consumed only after successful payment.
⦁	Duplicate payment webhooks do not change the order state more than once.
⦁	OTP validation is single‑use and time‑boxed; invalid or expired OTPs are rejected.