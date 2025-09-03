Analytics Tracking Spec (Accessory World)
Last updated: January 2025 - Enhanced Product Card & UI Analytics
Conventions
⦁	user_id for authenticated users, anon_id for guests.
⦁	All timestamps ISO-8601; currency=ZAR; amounts in cents.
Events (Enhanced with Product Card Analytics)

# Product Browsing & Card Interactions
product_grid_viewed(page_url, products_count, grid_layout, device_type)
product_card_viewed(product_id, position_in_grid, price_display_type, brand)
product_card_hovered(product_id, hover_duration_ms, position_in_grid)
product_card_clicked(product_id, click_target, position_in_grid, price_cents)
product_image_clicked(product_id, image_type, position_in_grid)
price_display_rendered(product_id, display_type, sku_count, min_price_cents, max_price_cents)
product_search_performed(query, results_count, filters_applied)
product_filter_applied(filter_type, filter_value, results_count)

# Enhanced Cart & Checkout
add_to_cart(sku, qty, price_cents, source, product_id, card_position)
cart_viewed(items_count, total_value_cents, session_duration_ms)
checkout_start(cart_value_cents, items_count, delivery_method)
payment_attempted(order_id, amount_cents, method, 3ds)
payment_succeeded(order_id, txn_id, amount_cents)

# Delivery & Fulfillment
delivery_selected(courier_code, eta_days, fee_cents)
pickup_selected(store_id, window_start, window_end)

# Trade-in & Credit
tradein_submitted(case_id, brand, model, imei_hash, channel)
tradein_offer_accepted(case_id, value_cents)
creditnote_redeemed(code_hash, order_id, value_cents)
refund_completed(order_id, amount_cents, reason)

# UI/UX Performance
page_load_time(page_type, load_time_ms, device_type, connection_type)
product_grid_render_time(products_count, render_time_ms, device_type)
image_load_error(product_id, image_url, error_type)
ui_interaction_error(element_type, error_message, page_url)
User Properties (Enhanced)
role, first_purchase_date, total_spend_cents, tradein_count, preferred_device_type, avg_session_duration_ms, product_cards_viewed_total, cart_abandonment_count, favorite_brands, price_range_preference

# Product Engagement Metrics
product_view_frequency, hover_engagement_rate, click_through_rate_by_position, preferred_price_display_type

# Conversion Funnel Properties
product_discovery_source, cart_conversion_rate, checkout_completion_rate, payment_method_preference