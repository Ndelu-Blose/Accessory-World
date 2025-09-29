# 32 — Payfast Redirect Build + Security



---


**Goal:** Generate Payfast redirect with correct signature and URLs.

### Inputs
- `PF_MERCHANT_ID`, `PF_MERCHANT_KEY`, `PF_PASSPHRASE`
- `RETURN_URL`, `CANCEL_URL`, `NOTIFY_URL`

### Steps
1) Build form fields: `merchant_id`, `merchant_key`, `amount`, `item_name`, `name_first`, `email_address`, `return_url`, `cancel_url`, `notify_url`.
2) Compute signature with ordered, URL‑encoded fields + passphrase.
3) Persist a **local `PaymentInit` record** linking `OrderId` and generated `m_payment_id` (your internal ref).
4) Redirect user (HTTP 303) to Payfast.

### Acceptance Criteria
- Signature matches Payfast expectations.
- `m_payment_id` is unique per attempt and mapped to `OrderId`.
