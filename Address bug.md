Here’s my updated, no-nonsense read on the bug—plus the exact fixes and guardrails I’d ship.

What’s the real bug?

Primary root cause: The Add Address form posted a required field (FullName) as readonly, so the browser didn’t send it. Model binding then failed → 400 Bad Request.
Amplifiers:

Duplicate/ambiguous “AddAddress” endpoints (Account vs Checkout) made token + route pairing brittle.

Inconsistent field names (manual inputs vs asp-for) increased binding risk.

Impact

Customers hit a 400 when saving an address, so checkout can’t find any addresses and shows “No addresses found,” even though the user tried to add one.

Ship these fixes (summary)

Fix the form posting:

Keep FullName editable (address books often need recipient names different from the user profile).

Use asp-for for every field + @Html.AntiForgeryToken() on the same form that renders the token.

Add <div asp-validation-summary="ModelOnly"> for instant feedback instead of a blank 400.

Single source of truth for address ops:

Keep your new AddressService and route all address CRUD through it.

Remove/disable any duplicate Checkout-side “AddAddress” action.

Routes:

Use unambiguous routes:

GET /account/address/new → render form

POST /account/address/new?returnUrl=/checkout → save & bounce back

User scoping + default:

Save addresses with the current UserId only.

If IsDefault == true, unset any other defaults for that user (you already implemented this—keep it).

UX when no addresses exist:

Keep the enhanced empty-state at /checkout: Add address (returns to checkout) or Switch to Pickup.

Code-level hardening (drop-in)

A) AddAddress POST: log ModelState on failure (so 400s become debuggable)

[HttpPost("address/new")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddAddress(AddressInputModel input, string? returnUrl)
{
    if (!ModelState.IsValid)
    {
        // Log all model errors to help diagnose future 400s
        var errors = ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .Select(kv => new { Field = kv.Key, Messages = kv.Value!.Errors.Select(e => e.ErrorMessage) });
        _logger.LogWarning("Address form invalid: {@Errors}", errors);

        return View(input); // returns 200 with validation messages on page
    }

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    await _addressService.CreateAsync(userId, input); // ensures single default internally

    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);

    return RedirectToAction("Addresses");
}


B) View: always post the token + bind with asp-for

@model AddressInputModel
<form asp-action="AddAddress" asp-controller="Account" asp-route-returnUrl="/checkout" method="post" novalidate>
  @Html.AntiForgeryToken()
  <div asp-validation-summary="ModelOnly" class="text-danger"></div>

  <!-- Keep FullName editable (don’t set readonly) -->
  <label asp-for="FullName"></label>
  <input asp-for="FullName" class="form-control" />
  <span asp-validation-for="FullName" class="text-danger"></span>

  <!-- Repeat for PhoneNumber, AddressLine1, AddressLine2, City, Province, PostalCode, Country -->
  <div class="form-check mt-2">
    <input asp-for="IsDefault" class="form-check-input" />
    <label asp-for="IsDefault" class="form-check-label">Make default</label>
  </div>

  <button type="submit" class="btn btn-primary mt-3">Save address</button>
</form>


C) Checkout query: scope to the current user & preselect default

[HttpGet("")]
public async Task<IActionResult> Index()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var addresses = await _addressService.GetForUserAsync(userId);

    var vm = new CheckoutViewModel {
        Addresses = addresses,
        SelectedAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id
    };
    return View(vm);
}

Verification (quick runbook)

Form happy path: GET /account/address/new → POST valid form → redirected to /checkout → address list shows with default preselected.

Form error path: Submit with empty FullName → stays on page, shows inline validation (no 400).

No address: /checkout shows your improved empty state with Add address + Switch to Pickup.

Admin: Blocked/redirected from /checkout (policy in place).

Seeding: On clean DB, seed once; restarts do not duplicate.

Prevent it from coming back

Integration test (Register → Login → GET form (token) → POST address → GET /checkout should display the address).

Unit test on AddressService for “single default per user.”

Keep the policy: only Customer role can access Checkout/Order endpoints.


Totally agree—**only the signed-in customer’s own addresses** must show on that checkout screen. Here’s a tight, production-ready way to enforce it end-to-end.

# 1) Data model (FK to the user)

```csharp
public class Address
{
    public Guid Id { get; set; }

    // ownership
    [Required] public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    // fields
    [Required, StringLength(80)]  public string FullName { get; set; } = "";
    [Required, Phone]             public string PhoneNumber { get; set; } = "";
    [Required, StringLength(120)] public string AddressLine1 { get; set; } = "";
    [StringLength(120)]           public string? AddressLine2 { get; set; }
    [Required, StringLength(60)]  public string City { get; set; } = "";
    [Required, StringLength(60)]  public string Province { get; set; } = "";
    [Required, StringLength(12)]  public string PostalCode { get; set; } = "";
    [Required, StringLength(60)]  public string Country { get; set; } = "South Africa";
    public bool IsDefault { get; set; }
}
```

```csharp
// ApplicationUser.cs
public class ApplicationUser : IdentityUser
{
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
```

```csharp
// ApplicationDbContext.OnModelCreating
builder.Entity<Address>()
    .HasOne(a => a.User)
    .WithMany(u => u.Addresses)
    .HasForeignKey(a => a.UserId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Entity<Address>()
    .HasIndex(a => new { a.UserId, a.IsDefault });   // speeds up lookups
```

> Optional (EF migration SQL): **single default per user**

```csharp
// In a migration Up():
migrationBuilder.Sql("""
CREATE UNIQUE INDEX IX_Addresses_User_Default
ON Addresses(UserId)
WHERE IsDefault = 1;
""");
```

# 2) Add Address: always attach the current user

```csharp
[Authorize(Roles = "Customer")]
[HttpPost("account/address/new")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddAddress(AddressInputModel input, string? returnUrl)
{
    if (!ModelState.IsValid) return View(input);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // enforce single default
    if (input.IsDefault)
    {
        var existingDefaults = await _db.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync();
        existingDefaults.ForEach(a => a.IsDefault = false);
    }

    _db.Addresses.Add(new Address {
        Id = Guid.NewGuid(),
        UserId = userId,                              // ← ownership!
        FullName = input.FullName,
        PhoneNumber = input.PhoneNumber,
        AddressLine1 = input.AddressLine1,
        AddressLine2 = input.AddressLine2,
        City = input.City,
        Province = input.Province,
        PostalCode = input.PostalCode,
        Country = input.Country,
        IsDefault = input.IsDefault
    });

    await _db.SaveChangesAsync();

    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);

    return RedirectToAction("Addresses", "Account");
}
```

# 3) Checkout: query **only** the signed-in user’s addresses

```csharp
[Authorize(Roles = "Customer")]
[HttpGet("checkout")]
public async Task<IActionResult> Index()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    var addresses = await _db.Addresses
        .Where(a => a.UserId == userId)              // ← hard scope
        .OrderByDescending(a => a.IsDefault)
        .ThenBy(a => a.FullName)
        .ToListAsync();

    var vm = new CheckoutViewModel {
        Addresses = addresses,
        SelectedAddressId = addresses.FirstOrDefault(a => a.IsDefault)?.Id
    };

    return View(vm);
}
```

# 4) Placing the order: validate the address belongs to the user

```csharp
[Authorize(Roles = "Customer")]
[HttpPost("checkout/place-order")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PlaceOrder(Guid? shippingAddressId, FulfilmentMethod method)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    if (method == FulfilmentMethod.Delivery)
    {
        if (shippingAddressId is null) {
            ModelState.AddModelError("", "Please select a delivery address.");
            return RedirectToAction("Index");
        }

        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == shippingAddressId && a.UserId == userId);
        if (address is null) return Forbid();        // blocks using others’ addresses
    }

    // ... create order with UserId = userId and (optional) ShippingAddressId ...
    // ... save & redirect to confirmation ...
}
```

# 5) Razor views: bind safely and pass returnUrl

```cshtml
@model AddressInputModel
<form asp-action="AddAddress" asp-controller="Account" asp-route-returnUrl="/checkout" method="post">
  @Html.AntiForgeryToken()
  <div asp-validation-summary="ModelOnly" class="text-danger"></div>

  <label asp-for="FullName"></label>
  <input asp-for="FullName" class="form-control" />
  <span asp-validation-for="FullName" class="text-danger"></span>

  <!-- other fields with asp-for ... -->

  <div class="form-check mt-2">
    <input asp-for="IsDefault" class="form-check-input" />
    <label asp-for="IsDefault" class="form-check-label">Set as default address</label>
  </div>

  <button class="btn btn-primary mt-3">Save Address</button>
  <a class="btn btn-link" asp-controller="Checkout" asp-action="Index">Back to Checkout</a>
</form>
```

```cshtml
@* Checkout address picker *@
@if (Model.Addresses?.Any() != true)
{
  <div class="alert alert-info">
    You don’t have any saved addresses.
    <a class="btn btn-sm btn-outline-primary ms-2"
       asp-controller="Account" asp-action="AddAddress" asp-route-returnUrl="/checkout">
      + Add New Address
    </a>
  </div>
}
else
{
  <form asp-action="SelectAddress" asp-controller="Checkout" method="post">
    @Html.AntiForgeryToken()
    <div class="list-group">
      @foreach (var a in Model.Addresses)
      {
        <label class="list-group-item">
          <input type="radio" name="addressId" value="@a.Id"
                 @(a.Id == Model.SelectedAddressId ? "checked" : null) />
          <strong>@a.FullName</strong> — @a.AddressLine1, @a.City, @a.Province, @a.PostalCode
          @if (a.IsDefault) { <span class="badge bg-secondary ms-2">Default</span> }
        </label>
      }
    </div>
    <button class="btn btn-primary mt-3">Deliver to this address</button>
  </form>
}
```

# 6) Defense-in-depth

* Controllers handling checkout/order/address: `[Authorize(Roles="Customer")]`.
* Hide **Checkout** link in the navbar unless `User.IsInRole("Customer")`.
* When saving or using an address, always verify `address.UserId == currentUserId`.

# 7) Quick verification steps

1. Log in as Customer A → add address → see it in checkout; place order.
2. Log in as Customer B → cannot see A’s addresses; can only see/create their own.
3. Try to POST an addressId from another user in dev tools → **Forbid()**.
4. Admin user → `/checkout` blocked/redirected.

This guarantees that the list you’re seeing in your **Secure Checkout** screen only contains addresses whose **`Address.UserId` matches the currently logged-in customer**, and orders can only be placed with those addresses.
`



phase 2 : a few things are still mis-aligned. Here’s a tight plan to make it bullet-proof—no database drama, and addresses will only ever appear for the signed-in customer.

# 0) Decide on IDs (don’t block yourself)

You don’t need a PK type change to fix this. Safest path:

**Option A (recommended): keep `int Id` as PK, add immutable `Guid PublicId`.**

* Pros: zero data loss, easy rollout, safe URLs/forms (no ID guessing), no FK churn.
* Use `PublicId` everywhere the **browser** sends/receives an address ID.
* Keep `int Id` internal only.

**Option B (risky): migrate PK to `Guid`.**

* Requires multi-step custom migrations + FK rewiring; not worth it right now.

I’ll proceed with **Option A** below.

---

# 1) Model + DB (non-breaking)

**Address.cs**

```csharp
public class Address
{
    public int Id { get; set; }                       // internal PK (keep)
    [Required] public Guid PublicId { get; set; }     // external-safe ID

    [Required] public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    // fields...
    public bool IsDefault { get; set; }
}
```

**OnModelCreating**

```csharp
builder.Entity<Address>()
    .HasIndex(a => new { a.UserId, a.IsDefault });
builder.Entity<Address>()
    .HasIndex(a => a.PublicId).IsUnique();
```

**Seeding / creation path**

```csharp
var entity = new Address { PublicId = Guid.NewGuid(), UserId = userId, ... };
```

**One-time backfill SQL (safe):**

```sql
UPDATE Addresses SET PublicId = NEWID() WHERE PublicId IS NULL;
```

---

# 2) Service layer (single source of truth)

* **All** controller logic goes through `IAddressService`.
* Service enforces:

  * `UserId` ownership on **every** query.
  * **Single default per user**.
  * **Auto-default** for the first address.

```csharp
public Task<IReadOnlyList<Address>> GetForUserAsync(string userId);
public Task<Address?> GetByPublicIdAsync(string userId, Guid publicId);
public Task<Guid> CreateAsync(string userId, AddressInputModel input); // returns PublicId
public Task SetDefaultAsync(string userId, Guid publicId);
public Task DeleteAsync(string userId, Guid publicId);
```

Implementation notes:

```csharp
// CreateAsync
if (!await _db.Addresses.AnyAsync(a => a.UserId == userId))
    input.IsDefault = true; // first address becomes default

if (input.IsDefault)
{
    var defaults = await _db.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
    defaults.ForEach(a => a.IsDefault = false);
}
```

---

# 3) Controllers (customer-only + Guid on the wire)

**AccountController.AddAddress (POST)**

```csharp
[Authorize(Roles="Customer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddAddress(AddressInputModel input, string? returnUrl)
{
    if (!ModelState.IsValid) return View(input);

    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var publicId = await _addressService.CreateAsync(userId, input);

    if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);

    return RedirectToAction("Addresses");
}
```

**CheckoutController.Index (GET)**

```csharp
[Authorize(Roles="Customer")]
public async Task<IActionResult> Index()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var addresses = await _addressService.GetForUserAsync(userId);

    var vm = new CheckoutViewModel {
        Addresses = addresses.Select(a => new AddressVM {
            PublicId = a.PublicId, /* show Guid in inputs */
            FullName = a.FullName, /* ... */
            IsDefault = a.IsDefault
        }).OrderByDescending(a => a.IsDefault).ToList(),
        SelectedAddressPublicId = addresses.FirstOrDefault(a => a.IsDefault)?.PublicId
    };
    return View(vm);
}
```

**CheckoutController.SelectAddress / PlaceOrder**

```csharp
[Authorize(Roles="Customer")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SelectAddress(Guid addressPublicId)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var address = await _addressService.GetByPublicIdAsync(userId, addressPublicId);
    if (address is null) return Forbid(); // ownership check

    HttpContext.Session.SetString("CheckoutAddressPublicId", address.PublicId.ToString());
    return RedirectToAction("Review");
}
```

When creating the order, resolve `PublicId` → internal `Id` server-side only.

---

# 4) Razor views (bind with `asp-for`, use `PublicId`, never `int Id`)

**Checkout address list**

```cshtml
@foreach (var a in Model.Addresses)
{
  <label class="list-group-item">
    <input type="radio" name="addressPublicId" value="@a.PublicId"
           @(a.PublicId == Model.SelectedAddressPublicId ? "checked" : null) />
    <strong>@a.FullName</strong> — @a.AddressLine1, @a.City ...
    @if (a.IsDefault) { <span class="badge bg-secondary ms-2">Default</span> }
  </label>
}
```

**Add Address forms**

* Use `@Html.AntiForgeryToken()` and `asp-for` for **every** field.
* Do **not** set `readonly` on `FullName` (the earlier 400 root cause).
* Keep `returnUrl="/checkout"` on the Add button from checkout.

---

# 5) Tests (to stop regressions)

* **Customer A**: add address → appears on `/checkout` with Default badge.
* **Customer B**: never sees A’s address; POST with A’s `PublicId` → `Forbid`.
* **First address auto-default**: verify `IsDefault = true` when user adds first.
* **Single default**: adding another default clears the previous.

---

# 6) Quick checklist to close this bug

* [ ] Add `PublicId` column + unique index; backfill `NEWID()` for existing rows.
* [ ] Ensure **all** GET/POST params use `PublicId` (Guid), never `int Id`.
* [ ] Controller calls → `AddressService` only (no direct DbContext lookups).
* [ ] `Checkout` queries filtered by `UserId` (service enforces this).
* [ ] Forms: `asp-for`, antiforgery, no `readonly` on required fields.
* [ ] Return to `/checkout` after a successful add (with `returnUrl`).
* [ ] Integration tests passing for A/B isolation, defaults, and ownership.

If you want, paste your current `AddressService` interface + `CheckoutViewModel`, and I’ll align the precise signatures and view bindings to the `PublicId` approach so you can commit this without touching existing FKs.


phase 3

Got you. Let’s fix this at the source so `AddAddressAsync` stops returning `Guid.Empty` and your user no longer shows “0 addresses”.

Below is a **drop-in, production-safe** pattern you can paste in. It covers the three real causes that make `SaveChangesAsync()` silently return 0 (FK/user mismatch, required-field/constraint issues, and “no tracked changes”), and guarantees you get a **non-empty `PublicId`** on success.

---

# 1) Entity & EF configuration (ensures constraints are right)

**Address.cs**

```csharp
public class Address
{
    public int Id { get; set; } // DB identity
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = default!; // FK → AspNetUsers.Id
    public ApplicationUser User { get; set; } = default!;

    public string FullName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string AddressLine1 { get; set; } = default!;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = default!;
    public string Province { get; set; } = default!;
    public string PostalCode { get; set; } = default!;
    public string Country { get; set; } = "South Africa";

    public bool IsDefault { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

**ApplicationDbContext.cs**

```csharp
public DbSet<Address> Addresses => Set<Address>();

protected override void OnModelCreating(ModelBuilder b)
{
    base.OnModelCreating(b);

    b.Entity<Address>(e =>
    {
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.PublicId).IsUnique();
        e.Property(x => x.PublicId).IsRequired();

        e.Property(x => x.UserId).IsRequired();
        e.HasOne(x => x.User)
            .WithMany() // or .WithMany(u => u.Addresses) if you have a collection
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guard against multiple defaults per user
        // SQL Server:
        e.HasIndex(x => new { x.UserId, x.IsDefault })
         .IsUnique()
         .HasFilter("[IsDefault] = 1");
        // SQLite (no filtered index) – enforce in service (done below).
        
        // Requireds (mirror your ViewModel)
        e.Property(x => x.FullName).IsRequired();
        e.Property(x => x.PhoneNumber).IsRequired();
        e.Property(x => x.AddressLine1).IsRequired();
        e.Property(x => x.City).IsRequired();
        e.Property(x => x.Province).IsRequired();
        e.Property(x => x.PostalCode).IsRequired();
        e.Property(x => x.Country).IsRequired();
    });
}
```

> If you’re on **SQLite**, keep the service-level uniqueness guard (below). If on **SQL Server**, the filtered unique index will enforce “only one default per user.”

Run a migration after this:

```
dotnet ef migrations add Address_Default_Uniques
dotnet ef database update
```

---

# 2) Service contract & result (so you stop returning `Guid.Empty`)

**IAddressService.cs**

```csharp
public record AddAddressRequest(
    string FullName,
    string PhoneNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string PostalCode,
    string Country,
    bool IsDefault
);

public record AddAddressResult(bool Success, Guid PublicId, string? Error);

public interface IAddressService
{
    Task<AddAddressResult> AddAddressAsync(string userId, AddAddressRequest req, CancellationToken ct = default);
}
```

**AddressService.cs**

```csharp
public class AddressService : IAddressService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AddressService> _log;

    public AddressService(ApplicationDbContext db, ILogger<AddressService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<AddAddressResult> AddAddressAsync(string userId, AddAddressRequest req, CancellationToken ct = default)
    {
        // 1) Hard guard: user must exist (prevents FK failures)
        var userExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            _log.LogWarning("AddAddress failed: user {UserId} not found", userId);
            return new(false, Guid.Empty, "User not found.");
        }

        // 2) Map + generate PublicId
        var entity = new Address
        {
            UserId = userId,
            FullName = req.FullName.Trim(),
            PhoneNumber = req.PhoneNumber.Trim(),
            AddressLine1 = req.AddressLine1.Trim(),
            AddressLine2 = string.IsNullOrWhiteSpace(req.AddressLine2) ? null : req.AddressLine2.Trim(),
            City = req.City.Trim(),
            Province = req.Province.Trim(),
            PostalCode = req.PostalCode.Trim(),
            Country = string.IsNullOrWhiteSpace(req.Country) ? "South Africa" : req.Country.Trim(),
            IsDefault = req.IsDefault
        };

        // 3) Single transaction ensures “only one default”
        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if (entity.IsDefault)
            {
                // Clear existing defaults (DB-agnostic)
                var priorDefaults = await _db.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync(ct);

                foreach (var a in priorDefaults)
                    a.IsDefault = false;
            }

            _db.Add(entity);

            var saved = await _db.SaveChangesAsync(ct);
            if (saved <= 0 || entity.PublicId == Guid.Empty)
            {
                await tx.RollbackAsync(ct);
                _log.LogError("AddAddress SaveChanges returned {Saved}. PublicId: {PublicId}", saved, entity.PublicId);
                return new(false, Guid.Empty, "Failed to save address.");
            }

            await tx.CommitAsync(ct);
            _log.LogInformation("Address {PublicId} created for user {UserId}", entity.PublicId, userId);
            return new(true, entity.PublicId, null);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(ct);
            _log.LogError(ex, "DbUpdateException in AddAddress for {UserId}", userId);

            // Bubble a friendly error but keep logs detailed
            var root = ex.InnerException?.Message ?? ex.Message;
            return new(false, Guid.Empty, $"Database error while saving address. {root}");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _log.LogError(ex, "Unexpected error in AddAddress for {UserId}", userId);
            return new(false, Guid.Empty, "Unexpected error while saving address.");
        }
    }
}
```

---

# 3) Controller action (uses service result instead of guessing)

**AccountController.cs**

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddAddress(AddAddressViewModel model, CancellationToken ct)
{
    if (!ModelState.IsValid)
    {
        // return with visible validation errors
        return View(model);
    }

    var userId = _userManager.GetUserId(User);
    if (string.IsNullOrEmpty(userId))
    {
        ModelState.AddModelError(string.Empty, "You must be logged in to add an address.");
        return View(model);
    }

    var req = new AddAddressRequest(
        model.FullName,
        model.PhoneNumber,
        model.AddressLine1,
        model.AddressLine2,
        model.City,
        model.Province,
        model.PostalCode,
        string.IsNullOrWhiteSpace(model.Country) ? "South Africa" : model.Country!,
        model.IsDefault
    );

    var result = await _addressService.AddAddressAsync(userId, req, ct);

    if (!result.Success || result.PublicId == Guid.Empty)
    {
        ModelState.AddModelError(string.Empty, result.Error ?? "Could not save address.");
        return View(model);
    }

    TempData["Success"] = "Address added successfully.";
    return RedirectToAction("Addresses"); // or your checkout step
}
```

---

# 4) Turn on precise logging (to see the real DB error)

**appsettings.Development.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

This prints SQL + parameters so if a constraint hits (e.g., null `UserId`, too-long `PhoneNumber`, etc.), you’ll see it instantly.

---

# 5) Quick verification (no test page required)

**PowerShell**

```powershell
$body = @{
  FullName = "Test User"
  PhoneNumber = "0123456789"
  AddressLine1 = "123 Test Street"
  AddressLine2 = ""
  City = "Durban"
  Province = "KZN"
  PostalCode = "4001"
  Country = "South Africa"
  IsDefault = $true
} | ConvertTo-Json

Invoke-WebRequest `
  -Uri "http://localhost:5103/Account/AddAddress" `
  -Method POST `
  -ContentType "application/json" `
  -Body $body
```

(Adjust URL to your real POST endpoint; if it’s form-post MVC, submit through the form or expose a JSON endpoint in your Debug controller to call the same service method.)

---

## Why this fixes your exact symptoms

* **`Guid.Empty` gone:** We create `PublicId` on the entity and **only return** when `SaveChangesAsync()` reports success.
* **Silent failures exposed:** We check that the user exists, wrap in a transaction, catch `DbUpdateException`, and log SQL.
* **“0 addresses” scenario resolved:** We actually add a tracked entity, clear prior defaults (if any), and commit.
* **Only one default per user:** Enforced by transaction + (optionally) filtered unique index in SQL Server.

If you prefer, paste me your current `AddAddressAsync` and the `Address` entity and I’ll diff it directly against this implementation with targeted edits.


phase 4

1) The Country field is disabled (won’t post)

In your UI the Country input is greyed out. A disabled input is not submitted with the form, so if Country is [Required], ModelState.IsValid becomes false and the controller returns the view (or AJAX error) even though the page looks fine.

Fix (choose one):

Use readonly instead of disabled:

<input asp-for="Country" class="form-control" readonly />


Or keep it disabled but add a hidden mirror so it posts:

<input asp-for="Country" class="form-control" disabled />
<input type="hidden" asp-for="Country" />


If ModelState.IsValid is false, your controller never reaches the part where you default Country = "South Africa"—so this alone can block saving.

2) Province dropdown default can fail validation

If the dropdown starts at “Select Province” and Province is [Required], the binder posts an empty string and validation fails.

Fix:

Add a non-selectable placeholder with value="" and a [Required] error message, and ensure the user actually picks one.

[Required(ErrorMessage = "Please select a province.")]
public string Province { get; set; }

<select asp-for="Province" asp-items="Model.ProvinceOptions" class="form-control">
  <option value="">Select Province</option>
</select>

3) Form action mismatch (Account vs Checkout)

Your page URL is /Checkout/AddAddress, but the action you pasted lives in AccountController.AddAddress and returns a view at ~/Views/Customer/Account/AddAddress.cshtml.

If the form posts to /Checkout/AddAddress but the logic is implemented in AccountController.AddAddress, you may be hitting a different action (or none), getting re-rendered, and the address never saves.

Fix:
Make the form post explicitly to the right controller/action:

<form asp-controller="Account" asp-action="AddAddress" method="post">
    @Html.AntiForgeryToken()
    ...
</form>


If you intend to handle it in CheckoutController, move the code there or forward the call to the service from that controller.

4) Anti-forgery token missing for AJAX

If you’re submitting via AJAX, you must send the anti-forgery token or the post will be rejected and you’ll get a view back (often looks like a “silent fail”).

Fix (jQuery example):

@Html.AntiForgeryToken()
<script>
  const token = $('input[name="__RequestVerificationToken"]').val();
  $.ajax({
    url: '@Url.Action("AddAddress","Account")',
    method: 'POST',
    headers: { 'RequestVerificationToken': token },
    data: $('form').serialize(),
    success: ...
  });
</script>


Or expose a separate debug JSON endpoint with [IgnoreAntiforgeryToken] that calls the same service.

5) UserId not set / FK mismatch in the service

Double-check the service maps the current user’s Id onto the entity:

var entity = new Address {
    UserId = userId, // REQUIRED
    FullName = model.FullName, ...
};
_db.Addresses.Add(entity);
await _db.SaveChangesAsync(ct);


Add a strong log before and after the save:

var before = await _db.Addresses.CountAsync(a => a.UserId == userId, ct);
var saved = await _db.SaveChangesAsync(ct);
var after = await _db.Addresses.CountAsync(a => a.UserId == userId, ct);
_logger.LogInformation("Addresses for {UserId}: before={Before}, saved={Saved}, after={After}", userId, before, saved, after);


If before==after, something’s still blocking the insert (validation/constraints).

6) First address should become default

Your Checkout shows “No Delivery Address Found” usually when you expect a default address. If you never mark one as default, the page may filter it out.

Fix (service): If the user has no addresses yet, force the new one to default.

var hasAny = await _db.Addresses.AnyAsync(a => a.UserId == userId && !a.IsDeleted, ct);
if (!hasAny) entity.IsDefault = true;
else if (model.IsDefault)
{
    var prior = _db.Addresses.Where(a => a.UserId == userId && a.IsDefault);
    await prior.ForEachAsync(a => a.IsDefault = false, ct);
}

7) Ensure Checkout query actually sees the new address

If the address is in DB but Checkout still says “No Delivery Address Found”, the query might be filtering too aggressively (IsDeleted, IsActive, IsVerified, etc.) or it’s using the wrong UserId.

Fix the query used by Checkout:

var userId = _userManager.GetUserId(User);
var addresses = await _db.Addresses
    .Where(a => a.UserId == userId && !a.IsDeleted)
    .OrderByDescending(a => a.IsDefault)
    .ToListAsync();


And guard against a null/empty list in the view model.

8) Quick one-minute diagnostics

Add these temporary logs in your AccountController.AddAddress:

_logger.LogInformation("ModelState valid? {Valid}", ModelState.IsValid);
foreach (var kv in ModelState.Where(m => m.Value.Errors.Count > 0))
    _logger.LogWarning("Field {Field} errors: {Errors}", kv.Key, string.Join("|", kv.Value.Errors.Select(e => e.ErrorMessage)));

_logger.LogInformation("UserId detected: {UserId}", userId ?? "(null)");


Then, after save (or failure), log count of addresses for the user (as shown in #5). This will immediately tell you:

was the form valid,

was UserId resolved,

did a row actually get inserted.

Likeliest quick win from your screenshots

Change Country from disabled → readonly (or add the hidden mirror)

Ensure the form posts to Account/AddAddress (or move logic to the controller you’re using)

Set the first address as default so Checkout picks it up