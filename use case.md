Use Cases - Updated January 2025

0) Browse Products with Enhanced Cards

Primary Actor: Customer (Visitor or Authenticated)
Stakeholders & Interests:

Customer: clear product information, professional presentation, easy comparison.

Business: effective product showcase, conversion optimization.

Preconditions: Products exist in catalog with valid SKUs and pricing.
Trigger: Customer visits shop/products page or searches for items.

Main Success Scenario:

System displays responsive product grid with professional card layout.

System validates SKU pricing data for each product.

For single SKU products: displays SKU.Price formatted as currency.

For multiple SKU products: displays price range (min-max) from valid SKUs.

System renders product cards with:
   - Primary product image with hover effects
   - Brand name and product title (XSS-safe)
   - Professional styling with shadows and spacing
   - Clear "Add to Cart" CTA buttons

Customer can hover over cards for enhanced visual feedback.

Customer can click product cards to view details or add to cart.

Extensions:

2a. No valid SKUs with pricing → hide product or show "Price on request".

4a. Missing primary image → display placeholder with brand logo.

6a. Out of stock → disable CTA and show "Out of Stock" status.

Postconditions: Products displayed with accurate pricing and professional presentation.

Business Rules: Only products with valid pricing data are shown; images must meet aspect ratio requirements.
NFRs: Grid renders <500ms; responsive design works on all screen sizes; hover effects smooth <100ms.

1) New Customer Signup

Primary Actor: Customer
Stakeholders & Interests:

Customer: quick, secure signup; privacy respected.

Business: valid contact details; fraud prevention.

Preconditions: Visitor not authenticated; email/phone not already registered.
Trigger: Customer clicks “Create account” or proceeds to checkout as new user.

Main Success Scenario:

System displays signup form (name, email, mobile, password).

Customer enters details and submits.

System validates format and uniqueness.

System sends verification OTP/link (SMS/email).

Customer verifies.

System creates account, establishes session, and lands on dashboard.

Extensions:

3a. Duplicate email/phone → System prompts login/“forgot password”.

4a. OTP expired/incorrect → allow resend with rate-limit.

5a. Link expired → reissue verification.

Postconditions: Verified customer account exists; audit trail written.

Business Rules: Strong passwords, unique email/phone, max 5 OTP attempts/hour.
NFRs: Verification <10s; PII encrypted at rest; 99.9% availability.

1.2) Product Price Display Logic

Primary Actor: System
Stakeholders & Interests:

Customer: accurate, clear pricing information.

Business: consistent pricing display, no pricing errors.

Preconditions: Product exists with associated SKUs.
Trigger: Product card rendering or detail page load.

Main Success Scenario:

System queries all active SKUs for the product.

System filters SKUs where Price > 0 and IsActive = true.

If single valid SKU: display "R [SKU.Price]" formatted as currency.

If multiple valid SKUs: calculate min/max prices, display "R [min] - R [max]".

System applies consistent currency formatting (ZAR with proper decimals).

Extensions:

2a. No valid SKUs → display "Price on request" or hide product.

4a. All SKUs same price → display as single price, not range.

5a. Currency conversion needed → apply current exchange rates.

Postconditions: Accurate pricing displayed to customer.

Business Rules: Only show products with valid pricing; price ranges must reflect actual SKU prices.
NFRs: Price calculation <50ms; currency formatting consistent across application.

1.5) Add Product to Cart

Primary Actor: Customer
Preconditions: Product has valid SKUs with stock; customer viewing product.
Trigger: Customer clicks "Add to Cart" from product card or detail page.

Main Success Scenario:

System validates product availability and stock levels.

System adds item to cart with current pricing snapshot.

System displays cart confirmation with updated totals.

System maintains cart state across session.

Extensions:

1a. Insufficient stock → show available quantity and adjust.

2a. Price changed since page load → update and notify customer.

4a. Guest user → create temporary cart tied to session.

Postconditions: Item added to cart with locked pricing; cart totals updated.

Business Rules: Cart items reserve stock for limited time; pricing locked until checkout.
NFRs: Add to cart response <200ms; cart state persists across browser sessions.

2) Place Order (Delivery)

Primary Actor: Customer
Preconditions: Authenticated; cart has in-stock items; shipping address available.
Trigger: Customer clicks “Checkout”.

Main Success Scenario:

System shows order summary, shipping address, courier options, fees, ETA.

Customer selects courier, confirms address, optionally adds note.

System snapshots prices, taxes, discounts, and reserves stock.

Customer chooses payment method and proceeds (see Use Case 3).

Payment succeeds; order becomes PAID.

System creates fulfilment task; sends confirmation to customer.

Extensions:

3a. Insufficient stock → adjust quantity or back-order (if enabled).

4a. Customer applies voucher/credit note → recalc totals (see Use Case 9/10).

5a. Payment pending (e.g., EFT) → status Awaiting Payment with instructions.

Postconditions: Order recorded with immutable price snapshot; fulfilment queued.

Business Rules: No price changes after snapshot; address must validate via geocoder.
NFRs: Checkout <3s render; payment initiation <2s.

3) Process Payment

Primary Actor: Payment Gateway (via customer)
Preconditions: Order totals computed; payment intent can be created.
Trigger: Customer clicks “Pay now”.

Main Success Scenario:

System creates payment intent with order ID, amount, currency.

Customer completes payment on gateway page/iframe.

Gateway sends idempotent webhook to System with result.

System verifies signature, amount, currency, order status.

On success: mark order PAID, store transaction ID, notify fulfilment.

On failure: mark Payment Failed, allow retry.

Extensions:

3a. Duplicate webhooks → ignored via idempotency key.

4a. Amount mismatch → flag as Payment Exception, hold order.

5a. 3-D Secure/OTP required → gateway step before success.

Postconditions: Payment outcome recorded; audit log captured.

Business Rules: One transaction ID per order; partial captures disabled unless split-ship.
NFRs: Webhook processing <1s; retries with exponential backoff.

4) Delivery Fulfilment

Primary Actor: Fulfilment Agent
Preconditions: Order status PAID; items locatable in inventory.
Trigger: New PAID order appears in fulfilment queue.

Main Success Scenario:

Agent prints pick list from system.

Agent picks items; system decrements allocated stock.

Agent packs and prints courier label (API).

System updates tracking number and status Ready for Dispatch.

Courier collects; system updates status In Transit.

Customer receives tracking link and notifications.

Delivery confirmed by courier scan → status Delivered.

Extensions:

2a. Pick discrepancy → create adjustment; escalate to Inventory Manager.

5a. Missed pickup → rebook; notify customer of new ETA.

Postconditions: Order delivered; proof of delivery stored if provided.

Business Rules: First-in-first-out by SLA; fragile items require special packaging.
NFRs: Label generation <2s; tracking updates near real-time.

5) Pickup Fulfilment (Click & Collect)

Primary Actor: Customer / Cashier
Preconditions: Order PAID; store has items ready.
Trigger: Customer selects Pickup during checkout.

Main Success Scenario:

System reserves store stock and generates a Pickup OTP and window.

Customer receives OTP and pickup instructions.

Customer arrives; Cashier requests OTP and ID (if high value).

System validates OTP (single-use, time-boxed).

Cashier hands over items; status Picked Up; receipt issued.

Extensions:

1a. No store stock → auto-transfer from warehouse; inform customer.

4a. OTP invalid/expired → resend/new window after ID verification.

Postconditions: Handover complete; audit entry with staff ID.

Business Rules: OTP validity (e.g., 24–72h); high-value orders require ID.
NFRs: OTP verification <300ms; offline fallback via manager override code.

6) Submit Trade-In

Primary Actor: Customer
Preconditions: Authenticated; trade-in program active.
Trigger: Customer opens “Trade-In” and submits device details.

Main Success Scenario:

System displays form (brand, model, IMEI/Serial, condition, photos, accessories).

Customer submits; system checks eligibility list and IMEI format.

System creates trade-in case with reference number.

Customer picks in-store inspection or courier evaluation.

System confirms next steps and SLA.

Extensions:

2a. Model not eligible → suggest recycle; end.

4a. Courier option → generate shipping label; hold until device received.

Postconditions: Trade-in case recorded and queued for evaluation.

Business Rules: One active case per device ID/IMEI; honest condition disclosure required.
NFRs: Photo upload tolerant to mobile bandwidth; virus scan on images.

7) Approve/Reject Trade-In (Valuation)

Primary Actor: Inventory Manager / Technician
Preconditions: Trade-in case exists; device received or presented in store.
Trigger: Case status Awaiting Evaluation.

Main Success Scenario:

Technician inspects device (power-on, screen, battery health, iCloud/FRP lock).

System guides through graded checklist to produce a condition score.

Manager sets offer value based on grade & pricing matrix.

System notifies customer with offer and expiry date.

Customer accepts digitally (click/OTP).

System marks case Approved, device becomes inventory (used/refurb bin).

System issues Credit Note to customer (see Use Case 9).

Extensions:

1a. Device locked/blocked → Reject, return device.

5a. Customer declines or expiry reached → Rejected/Expired; notify.

Postconditions: Decision recorded; device disposition set; audit log complete.

Business Rules: Mandatory proof of ownership for high-value devices; data-wipe before inventorying.
NFRs: Evaluation flow <10 mins; matrix configurable without deploy.

8) Manage Inventory

Primary Actor: Inventory Manager
Preconditions: Proper role permissions.
Trigger: New product intake, price change, or cycle count.

Main Success Scenario:

Manager creates/edits products, SKUs, variants, prices, barcodes.

System logs stock movements (receipts, adjustments, sales, returns).

System enforces non-negative stock (unless back-order enabled).

Thresholds trigger low-stock alerts and replenishment suggestions.

Extensions:

2a. Mismatch during cycle count → create adjustment with reason code.

3a. Back-order enabled → allocate on future PO.

Postconditions: Inventory accurate; movement ledger up-to-date.

Business Rules: All movements require reason codes; pricing requires dual control over RRP drops >15%.
NFRs: Bulk imports 5k rows <30s; history immutable.

9) Issue Credit Note

Primary Actor: System (after approval) / Admin
Preconditions: Trade-in Approved or Admin-initiated credit.
Trigger: Completion of Use Case 7 or manual issuance.

Main Success Scenario:

System generates a single-use, customer-bound Credit Note with value and expiry.

System stores code securely and associates to customer ID.

Customer is notified with value, terms, and how to redeem.

Extensions:

1a. Admin override for expiry/amount → second person approval required.

Postconditions: Credit note available for redemption at checkout.

Business Rules: Single-use only; non-transferable; cannot exceed order total; combinability rules defined.
NFRs: Redemption lookup <100ms; collision-free code space.

10) Redeem Credit Note (at Checkout)

Primary Actor: Customer
Preconditions: Valid, unused credit note tied to customer; items in cart.
Trigger: Customer enters credit note code at payment step.

Main Success Scenario:

System validates code, customer binding, expiry, and status.

System applies value to order total; recalculates taxes/fees.

If balance remains, customer completes payment (Use Case 3).

System marks note Consumed upon successful payment/finalization.

Extensions:

1a. Note expired/used/wrong customer → clear error; allow continue without note.

2a. Order total < note value → cap at total; no cash out.

Postconditions: Order placed; credit note consumed and audited.

Business Rules: One note per order (if decided) or limit N; no stacking with certain promos.
NFRs: Validation constant-time to resist enumeration.

11) Process Refund

Primary Actor: Admin / Payment Gateway
Preconditions: Original payment captured; within policy window.
Trigger: Customer or Admin initiates refund.

Main Success Scenario:

Admin selects order lines/amount to refund (full/partial).

System validates window and return status if goods-based.

System calls gateway refund API with reference and amount.

Gateway returns success; System marks items Refunded.

System updates stock if items returned to saleable condition.

Customer is notified; audit log stored.

Extensions:

3a. Gateway error → retry with backoff; escalate on persistent failure.

5a. Non-saleable return → move to “salvage” bin; no stock increment.

Postconditions: Refund recorded; financials reconciled.

Business Rules: Refund to original tender; RMA number required for returns.
NFRs: Refund initiation <2s; reconciliation report daily.

12) Log Stock Movement (Ledger)

Primary Actor: System
Preconditions: Any inventory-affecting operation occurs.
Trigger: Sales, receipts, adjustments, returns, transfers.

Main Success Scenario:

Operation triggers a movement event with SKU, qty, location, reason.

System writes immutable ledger entry with user/process ID and timestamp.

Reports and dashboards consume the ledger for stock on hand and aging.

Extensions:

2a. Write failure → retry; if still failing, raise SEV-2 and queue events.

Postconditions: Single source of truth for inventory movements.

Business Rules: No manual ledger edits; corrections via compensating entries only.
NFRs: Event write p95 <50ms; eventual consistency <2s.