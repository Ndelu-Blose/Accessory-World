# API Contracts (JSON)

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Endpoints
### POST /api/tradeins
Request:
```json
{ "deviceModel":"iPhone 11", "imei":"", "conditionGrade":"B", "photos":[ "url1","url2" ], "notes":"" }
```
Response: `201 Created`
```json
{ "tradeInId":"GUID", "publicId":"GUID", "status":"Submitted" }
```

### POST /api/admin/tradeins/{id}/approve
```json
{ "approvedValue": 3250.00 }
```
→ `200 OK` with TradeIn view.

### POST /api/admin/tradeins/{id}/credit-note
```json
{ "expiresAt":"2026-03-31T00:00:00Z" }
```
→ `201 Created`
```json
{ "code":"CN-7F9D2K", "amount":3250.00, "expiresAt":"2026-03-31T00:00:00Z" }
```

### POST /api/checkout/apply-credit-note
Headers: `Idempotency-Key: <uuid>`
```json
{ "code":"CN-7F9D2K", "orderId":"GUID", "orderTotal": 4999.00 }
```
→ `200 OK`
```json
{ "applied": true, "discount": 3250.00, "lockedToOrderId":"GUID" }
```

Error codes:
- 400 invalid_state | 401 unauthorized | 403 forbidden | 404 not_found | 409 conflict | 422 validation_failed

## Acceptance
- Swagger shows exact schemas; error responses include `code` and `message`.
