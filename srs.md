Accessory World — Software Requirements Specification (SRS)
Last updated: January 2025 - Enhanced Product Display & UI
1. Introduction
This SRS defines functional and non‑functional requirements for the Accessory World e‑commerce + trade‑in platform (South Africa).
2. Actors
Customer, Admin, Inventory Manager, Fulfilment Agent, Cashier, Payment Gateway, Courier.
3. Detailed Use Cases (v2 - Enhanced Product Display)
(Each UC includes Preconditions, Trigger, Main Flow, Extensions, Postconditions, Business Rules, NFRs.)

UC‑00: Browse Products with Enhanced Cards
Preconditions: Products exist with valid SKUs and pricing data.
Trigger: Customer visits shop/products page.
Main Flow: Display responsive grid → validate SKU pricing → render professional cards (image, brand, title, price) → handle hover effects → enable cart actions.
Extensions: No valid SKUs → hide/show "Price on request"; missing images → placeholder; out of stock → disable CTA.
Post: Products displayed with accurate pricing and professional presentation.
Rules: Only show products with valid pricing; XSS protection on titles; responsive design.
NFRs: Grid render <500ms; hover effects <100ms; mobile-responsive.
UC‑01: New Customer Signup
Preconditions: Not authenticated; unique email/phone.  
Trigger: Click “Create account”.  
Main Flow: Fill form → validate → send OTP/link → verify → create account + session.  
Extensions: Duplicate email; OTP expired; resend with rate‑limit.  
Post: Verified account; audit trail.  
Rules: Strong password; unique email/phone; ≤5 OTP attempts/hour.  
NFRs: Verification <10s; PII encrypted; 99.9% uptime.
UC‑02: Place Order (Delivery)
Preconditions: Authenticated; cart has stock; shipping address.  
Trigger: “Checkout”.  
Main Flow: Address/Delivery → snapshot totals & reserve stock → payment → PAID → fulfilment task.  
Extensions: Insufficient stock; voucher/credit note; EFT pending.  
Post: Immutable price snapshot; fulfilment queued.  
Rules: No price changes post‑snapshot; address must validate.  
NFRs: Checkout render <3s; pay initiation <2s.
UC‑03: Process Payment
Preconditions: Order totals computed.  
Trigger: “Pay now”.  
Main Flow: Create payment intent → customer completes at gateway → signed webhook → verify → mark PAID / Failed.  
Extensions: Duplicate webhooks; amount mismatch; 3DS step‑up.  
Post: Payment outcome recorded; audit log.  
Rules: One transaction ID per order.  
NFRs: Webhook ≤1s; retries with backoff.
UC‑04: Delivery Fulfilment
Pre: Order PAID. Trigger: In queue.  
Main: Pick list → pick → pack → print label → tracking → Delivered.  
Ext: Pick discrepancy; missed pickup.  
Post: POD stored. Rules: FIFO by SLA; fragile packaging. NFRs: Label <2s.
UC‑05: Pickup Fulfilment
Pre: PAID; stock at store.  
Main: Generate OTP & window → customer arrives → OTP/ID → Picked Up.  
Ext: No store stock; OTP invalid/expired.  
Rules: OTP 24–72h validity; high‑value ID. NFRs: OTP check <300ms.
UC‑06: Submit Trade‑In
Pre: Authenticated.  
Main: Form (model, IMEI, photos) → validate eligibility → case created → choose evaluation (store/courier) → SLA shown.  
Ext: Not eligible; courier label generated.  
Rules: One active case per IMEI. NFRs: Mobile‑friendly uploads.
UC‑07: Approve/Reject Trade‑In
Pre: Case exists; device inspected.  
Main: Checklist grading → matrix value → offer sent → accept with OTP → mark Approved → add to inventory → issue credit note.  
Ext: Locked/blocked device → reject; expiry reached.  
Rules: Ownership proof; data wipe. NFRs: Flow <10 mins.
UC‑08: Manage Inventory
Main: CRUD products/SKUs; movements; thresholds & alerts.  
Rules: Reason codes; dual‑control for price cuts >15%.
UC‑09: Issue Credit Note
Main: Generate single‑use, bound to customer, with expiry; notify.  
Rules: Non‑transferable; cannot exceed order total.
UC‑10: Redeem Credit Note
Main: Validate binding/expiry → apply to total → pay balance → mark Consumed on success.  
Ext: Expired/used/wrong owner → reject.  
NFR: Constant‑time lookup to resist enumeration.
UC‑11: Process Refund
Main: Select lines/amount → validate policy → gateway refund → mark Refunded → update stock if saleable.  
Ext: Gateway error → retry; non‑saleable → salvage bin.  
Rules: Original tender; RMA required.
UC‑12: Log Stock Movement
Main: Write immutable ledger entry on each inventory event; p95 write <50ms.
4. Functional Requirements (Enhanced)
FR‑00 Product Card Display System:
  - Professional card layout with shadows, spacing, and hover effects
  - Price display logic: single price vs. range based on valid SKUs
  - Primary image display with aspect ratio validation
  - Brand and product title with XSS protection
  - Responsive grid system for all screen sizes
  - "Add to Cart" CTA with stock validation

FR‑01 Account verification via SMS/email OTP.
FR‑02 Address validation & courier quote before payment.
FR‑03 Payment webhooks (signed) with idempotency keys.
FR‑04 Trade‑in grading checklist with configurable matrix.
FR‑05 Credit note single‑use enforcement and customer binding.
FR‑06 OTP for pickup (single‑use, time‑boxed).
FR‑07 Stock movement ledger with reason codes.
FR‑08 Refund to original tender via gateway API.
FR‑09 Role‑based access control (RBAC) with audit trails.
FR‑10 Email/SMS notifications driven by templates/locale.
FR‑11 Enhanced Cart Operations:
  - Real-time stock validation on add to cart
  - Price consistency checks during session
  - Cart state persistence across browser sessions
  - Guest cart functionality with session management
5. Non‑Functional Requirements (Enhanced)
Performance:
  - Product grid rendering <500ms
  - Price calculation and display <50ms
  - Add to cart response <200ms
  - Hover effects and animations <100ms
  - Image loading with progressive enhancement

UI/UX:
  - Responsive design for mobile, tablet, desktop
  - Professional visual hierarchy with consistent spacing
  - Accessible color contrast ratios (WCAG 2.1 AA)
  - Touch-friendly interface elements (44px minimum)
  - Smooth animations and transitions

Security:
  - XSS protection on all user-generated content
  - CSRF tokens on all forms
  - Input validation and sanitization
  - Secure image upload handling
  - Price tampering protection

Availability, security/POPIA/PCI, observability, scalability — see PRD §5.
6. Data Model (Enhanced)
See ERD.mmd. Key entities: User, Address, Product, SKU, Order, OrderItem, Payment, Shipment, PickupOTP, TradeInCase, CreditNote, StockMovement, RMA, Role, AuditLog.

Enhanced Product Display Requirements:
- Product.Name: required, ≤100 chars, XSS-safe
- Product.Brand: required, valid foreign key
- Product.Images: ≥1 with exactly one IsPrimary=true
- SKU.Price: >0 for display, decimal precision validation
- SKU.IsActive: boolean for availability filtering
- ProductImage.AspectRatio: validated dimensions
- Product.Description: sanitized HTML, length limits
7. RBAC Matrix (excerpt)
⦁	Admin: all business functions.
⦁	Inventory Manager: products, SKUs, counts, adjustments, valuations.
⦁	Fulfilment Agent/Cashier: pick/pack, OTP verification, handover.
⦁	Read‑only: reporting.
8. Interfaces & Integrations
⦁	Payfast (payments + webhooks), Courier API (labels + tracking), SMS/Email provider, Geocoder.
9. Acceptance Criteria
Mapped in RTM.csv.