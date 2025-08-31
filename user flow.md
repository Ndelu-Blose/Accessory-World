User Flow (High-Level)
[Landing/Home]
   ├─> Browse Catalog → Search/Filter → Product Detail
   │        └─> Add to Cart → View Cart
   └─> Promo/Deep Link → Product Detail → Add to Cart

[Cart]
   └─> Proceed to Checkout
         ├─> Auth Gateway:
         │     ├─ Sign In
         │     ├─ Sign Up (OTP/Email Verify)
         │     └─ Guest → (optional) create account post-order
         ├─> Addresses:
         │     ├─ Add/Select Shipping Address
         │     └─ (If Pickup) Select Store + Time Window
         ├─> Delivery Method:
         │     ├─ Courier (ETA/Fee)
         │     └─ Pickup (store/slot, OTP to phone/email)
         ├─> Order Review:
         │     ├─ Apply Voucher/Promo
         │     ├─ Apply Credit Note (from Trade-In)
         │     └─ Snapshot Prices/Tax/Stock
         └─> Payment:
               ├─ Choose Method → Pay
               ├─ Gateway Auth/3DS → Webhook → Success?
               │     ├─ Yes → Order = PAID
               │     └─ No  → Retry / Change Method
               └─> Confirmation (Order #, email/SMS)

[Fulfilment]
   ├─ Delivery:
   │     ├─ Pick/Pack → Print Label → Handover to Courier
   │     └─ Tracking → In Transit → Delivered (POD)
   └─ Pickup:
         ├─ Prepare Parcel → Notify Ready + OTP
         └─ Customer Arrives → OTP/ID Check → Hand-over → Picked Up

[Post-Purchase]
   ├─ Track Order (notifications)
   ├─ Returns/Refunds (RMA → Inspect → Refund via Gateway)
   └─ Account: invoices, addresses, saved payment, history

[Trade-In Path]  (can start before or after purchase)
   ├─ Submit Device (model, IMEI, photos, condition)
   ├─ Choose Evaluation: In-Store / Courier
   ├─ Technician Grades → Offer (with expiry)
   │     ├─ Accept → Issue Credit Note (single-use, bound to user)
   │     └─ Decline/Expired → Close Case (return device if applicable)
   └─ Redeem Credit Note at Checkout (above flow)

Swimlane-Style Detail (key decisions inline)

Customer

Arrive → Browse/Search → Product Detail → Add to Cart

Checkout → Auth (Sign in / Sign up / Guest)

Provide Address (or choose Pickup store/slot)

Choose Delivery Method (Courier vs Pickup)

Review order → Apply Voucher/Credit Note → Pay

Receive confirmation + notifications

If Delivery: track parcel → receive order
If Pickup: present OTP/ID → collect

Post-purchase: track/return/refund as needed

Optional: start Trade-In → accept offer → redeem note next order

System

Validates forms, uniqueness (signup), and stock at checkout

Snapshots prices/taxes/discounts, reserves stock

Creates Payment Intent, listens for idempotent webhook

Marks order PAID (on verified webhook), enqueues fulfilment

Generates label (delivery) or OTP (pickup), sends notifications

Logs inventory movements, audits all key actions

Trade-in: creates case, guides evaluation, issues Credit Note, ties it to user

Payment Gateway

Collects card/Alt-pay + 3DS if needed

Sends signed webhook (success/fail) → retried on failure

Fulfilment / Store

Pick/Pack, label, staging; or prepare pickup parcel

Hand-over to Courier (delivery) or Customer (pickup)

Update status scans (Ready → In Transit → Delivered / Picked Up)

Courier

Collect, transport, update tracking, confirm POD

Admin/Inventory

Manage products/SKUs; reconcile stock; handle RMAs and refunds

Key Decision Points

Auth path: Sign in vs Sign up vs Guest (affects post-order account linking)

Fulfilment: Courier vs Pickup (OTP branch)

Discounting: Promo vs Credit Note (rules on stacking)

Payment outcome: Success → fulfilment; Fail → retry/change method

Trade-in: Accept offer → note issued; Decline/expire → case closed

flowchart TD
  A[Landing/Home] --> B[Browse/Search]
  B --> C[Product Detail]
  C --> D[Add to Cart]
  D --> E[Cart]
  E --> F[Checkout]
  F --> G{Auth?}
  G -->|Sign In| H[Session Ready]
  G -->|Sign Up| I[OTP Verify]
  G -->|Guest| H
  H --> J{Delivery Method}
  J -->|Courier| K[Address & Quote]
  J -->|Pickup| L[Select Store & Slot / OTP]
  K --> M[Order Review & Apply Credit Note]
  L --> M
  M --> N[Create Payment Intent]
  N --> O[Gateway Pay]
  O --> P{Webhook}
  P -->|Success| Q[Order = PAID]
  P -->|Fail| N
  Q --> R{Fulfilment}
  R -->|Courier| S[Pick/Pack → Label → Tracking → Delivered]
  R -->|Pickup| T[Prepare Parcel → OTP Verify → Picked Up]
  Q --> U[Notifications & Account]
