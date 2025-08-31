Analytics Tracking Spec (Accessory World)
Last updated: 2025-08-25
Conventions
⦁	user_id for authenticated users, anon_id for guests.
⦁	All timestamps ISO-8601; currency=ZAR; amounts in cents.
Events
add_to_cart(sku, qty, price_cents, source)
checkout_start(cart_value_cents, items_count, delivery_method)
payment_attempted(order_id, amount_cents, method, 3ds)
payment_succeeded(order_id, txn_id, amount_cents)
delivery_selected(courier_code, eta_days, fee_cents)
pickup_selected(store_id, window_start, window_end)
tradein_submitted(case_id, brand, model, imei_hash, channel)
tradein_offer_accepted(case_id, value_cents)
creditnote_redeemed(code_hash, order_id, value_cents)
refund_completed(order_id, amount_cents, reason)
User Properties
role, first_purchase_date, total_spend_cents, tradein_count