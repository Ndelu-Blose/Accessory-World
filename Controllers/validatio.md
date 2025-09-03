Here's a detailed, practical description of each validation layer—what it is, what failures it prevents, where to implement it, and a quick .NET pattern you can use right now. **Updated January 2025** with current validation rules, product card validation, and enhanced security measures.

1) Input & Model Validation (syntax/shape)

What it is: Checks that incoming data has the right fields and formats before any business logic runs.
Prevents: Null/empty fields, bad formats (email/phone/URL), out-of-range values, broken JSON/forms.
Where: At the API boundary (controllers/minimal APIs) and view models/DTOs.
How in .NET:

Data Annotations: [Required], [StringLength], [Range], [EmailAddress], [RegularExpression].

Object-level rules: IValidatableObject for cross-field checks.

FluentValidation: Rich rules + better messages, reusable per DTO.

Pipeline: [ApiController] auto 400 on invalid ModelState.

2) Domain & Business-Rule Validation (meaning)

What it is: Guards your core invariants—the “truths” of your business—regardless of UI or API.
Prevents: Selling more than stock, illegal order transitions, duplicate primary images, changing immutable keys.
Where: Domain/Application services, not controllers; enforce again in aggregates/entities if you use DDD.
How in .NET:

Throw domain exceptions (DomainException) when invariants fail.

Encapsulate operations (e.g., Order.AddItem, Order.Pay) so rules live with behavior.

Use policies/state machines (e.g., Stateless library) to model allowed transitions.

3) Persistence & Data-Integrity Validation (database)

What it is: Let the DB enforce invariants even if app code misses one.
Prevents: Duplicate keys, orphan rows, negative prices/stock, lost updates.
Where: EF Core model configuration & migrations.
How in .NET/EF Core:

FKs/Required: .IsRequired(); proper relationships.

Unique indexes: .HasIndex(x => x.SKUCode).IsUnique();

Check constraints: .HasCheckConstraint("CK_Sku_Stock", "[StockQuantity] >= 0");

Optimistic concurrency: [Timestamp] / .IsRowVersion() on a byte[] RowVersion.

4) Security Validation

What it is: Validates identity, permissions, and hostile inputs.
Prevents: XSS/CSRF, privilege escalation, poisoned file uploads, account abuse.
Where: Middleware, controllers, upload endpoints, Razor pages.
How in .NET:

AuthN/AuthZ: [Authorize(Policy="AdminOnly")], per-resource checks.

Anti-forgery: @Html.AntiForgeryToken() + ValidateAntiForgeryToken.

Sanitization/encoding: Never trust HTML; store as text or sanitize first.

File uploads: Check MIME/extension/size; optionally virus-scan; store outside webroot or use object storage.

5) Workflow / State Validation

What it is: Ensures processes advance legally and once.
Prevents: Skipping steps, double-charging, double-shipping, inconsistent stock reservations.
Where: Application layer + persistence transaction boundaries.
How in .NET:

State machine: Guard transitions (e.g., Pending → Paid → Shipped → Completed).

Idempotency: Require a RequestId and ignore duplicates.

Transactions: await using var tx = await db.Database.BeginTransactionAsync(); Commit only when all steps pass.

6) Payment Validation (Payfast, etc.)

What it is: Verifies webhooks/ITN authenticity and financial consistency.
Prevents: Fake payment notifications, amount tampering, repeated settlements.
Where: Payment callback endpoints + payment service.
How in .NET:

Signature/HMAC: Recompute with your passphrase; reject mismatches.

Amount/currency checks: Compare callback to order snapshot.

Merchant IDs: Validate against config.

Idempotency: Persist pf_payment_id; ignore if seen.

Status mapping: Only mark Paid after all checks succeed.

7) API Contract Validation

What it is: Keep producer/consumer aligned and predictable.
Prevents: Breaking changes, ambiguous errors, client regressions.
Where: API layer + documentation.
How in .NET:

DTOs with annotations exposed via Swagger/OpenAPI.

Versioning: AddApiVersioning(); route /v1, /v2.

Errors: Return ProblemDetails with machine-readable codes.

8) Performance / Abuse Validation

What it is: Guardrails on resource use and query shapes.
Prevents: Slow queries, DoS via huge pages, arbitrary SQL/order-by injections.
Where: Controllers, query layer, HTTP server config.
How in .NET:

Enforce pageSize caps, allowed sort fields.

Kestrel limits (request body size, concurrent connections).

Timeouts + cancellation tokens on external calls.

Cache validation: ETags/If-None-Match where safe.

9) Configuration & Startup Validation

What it is: Fail fast when critical config is wrong.
Prevents: Silent misconfig (bad keys, missing URLs, null secrets).
Where: Program startup.
How in .NET:

IOptions<T> with .ValidateDataAnnotations().ValidateOnStart().

Custom validators to assert non-empty keys/URLs.

Health checks for dependencies (DB, queue, storage).

10) UX Validation (client side)

What it is: Early, friendly checks before hitting the server.
Prevents: Frustration and round-trips for obvious mistakes.
Where: Browser (HTML5+JS), mobile clients.
How:

HTML attributes: required, min, max, pattern.

Mirror server rules to reduce drift (share schema or emit constraints via Swagger and generate clients).

Client validation helps UX; server validation is authoritative.

11) Observability Validation

What it is: Prove your rules work and spot pain points.
Prevents: Blind spots; lets you tune rules and catch attacks.
Where: Logging, metrics, dashboards, alerts.
How:

Structured logs on validation failures (no PII).

Metrics: Count 400s per endpoint, top reasons (e.g., invalid SKU, insufficient stock).

Alerts: Spike in failed payment signatures, file-upload rejections, etc.

Minimal Starter Matrix (tailored to AccessoryWorld) - Current Implementation

Product (Enhanced Validation)

Name: required, ≤100 characters, no HTML/script injection

Price: ≥0 (DB check), currency enforced, proper decimal precision

Brand/Category: valid FKs, required fields

Images: ≥1; exactly one IsPrimary = true (domain + DB unique filtered index)

Description: sanitized HTML, length limits

SEO fields: meta title/description validation

Product Card Display Validation:

- Price formatting: validates single price vs. price range logic
- SKU availability: ensures valid pricing data before display
- Image aspect ratios: validates proper image dimensions
- Brand/name display: prevents XSS in product titles

SKU (Enhanced with Price Display Logic)

Unique SKUCode (DB unique), alphanumeric format validation

Stock/Reserved/Threshold ≥0 (checks), integer validation

Price rules vs Product (if coupled), decimal precision validation

Price Display Logic: validates SKU.Price > 0 for proper price formatting

Conditional Pricing: handles single price vs. range display based on valid SKUs

RowVersion for concurrency, optimistic locking

Cart (Enhanced Validation)

Each line qty ≥ 1, maximum quantity limits per product

qty ≤ available stock (domain), real-time stock validation

Recalculate totals server-side, tax calculation validation

Product availability: validates product still exists and is active

Price consistency: ensures prices haven't changed since adding to cart

Session validation: prevents cart manipulation across sessions

Checkout (Enhanced Security)

Address required; phone/email formats, postal code validation

Lock prices/taxes at checkout snapshot, currency validation

Transaction wraps stock reserve + order create, atomic operations

Payment method validation: ensures valid payment options

Shipping validation: validates delivery addresses and methods

Order total validation: prevents price manipulation

CSRF protection: anti-forgery tokens on all checkout forms

Payment (Enhanced Security)

Verify signature, amount, currency, merchant IDs

Idempotent ITN handling, duplicate payment prevention

State transition only on verified success, audit logging

Payfast integration: webhook signature validation

Amount tampering protection: server-side amount verification

Payment status validation: proper state machine implementation

Refund validation: ensures proper refund authorization

Uploads (Enhanced Security)

Types: jpeg/png/webp, SVG validation for vector graphics

Size cap (2MB for images, 5MB for documents)

Dimensions validation: minimum/maximum image sizes

File content validation: MIME type verification beyond extension

Virus scanning: integration with antivirus services

Path traversal protection: sanitized file names and paths

Image optimization: automatic compression and format conversion

Fast .NET implementation snippets

EF Core constraints

modelBuilder.Entity<ProductImage>()
    .HasIndex(pi => new { pi.ProductId, pi.IsPrimary })
    .IsUnique()
    .HasFilter("[IsPrimary] = 1");

modelBuilder.Entity<SKU>()
    .HasIndex(s => s.SKUCode)
    .IsUnique();

modelBuilder.Entity<SKU>()
    .HasCheckConstraint("CK_SKU_Stock", "[StockQuantity] >= 0 AND [ReservedQuantity] >= 0");


DTO + FluentValidation

public class AddToCartDto { 
    public string SkuCode { get; set; } = default!;
    public int Quantity { get; set; }
}
public class AddToCartValidator : AbstractValidator<AddToCartDto> {
    public AddToCartValidator() {
        RuleFor(x => x.SkuCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100);
    }
}


Domain guard (stock)

if (sku.AvailableQuantity() < dto.Quantity)
    throw new DomainException("Insufficient stock");


Payment ITN idempotency

if (await db.PaymentEvents.AnyAsync(e => e.ProviderId == pfPaymentId))
    return Ok(); // already processed