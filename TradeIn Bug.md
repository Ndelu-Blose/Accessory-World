# TRAE FIX PACK — Trade-In page crash (`Invalid column name …`)

## 0) What’s failing (from your screenshots)

`/TradeIn` throws:

* `Invalid column name 'TradeInId'`
* `Invalid column name 'AmountRemaining'`
* `Invalid column name 'ApplicationUserId'`
* `Invalid column name 'RedeemedAt'`
* `Invalid column name 'RedeemedOrderId'`
* `Invalid column name 'RowVersion'`
* `Invalid column name 'TradeInCaseId1'`

The stack shows it fails during `TradeInService.GetUserTradeInsAsync(userId)` when EF queries Trade-In data.

---

## 1) Why this is happening (specific to your models)

1. **DB schema is behind the models**
   Your entity classes include properties that the current SQL database does **not** have yet (e.g., `TradeInId`, `AmountRemaining`, `RedeemedAt`, `RedeemedOrderId`, `RowVersion`). That’s a migrations/application-DB mismatch.

2. **Shadow FK → `TradeInCaseId1`**
   `CreditNote` has `TradeInCaseId` and a navigation `TradeInCase`, and `TradeInCase` has `ICollection<CreditNote>`. Because FK/navigation pairing isn’t configured explicitly, EF can create a **second, shadow FK** (named `TradeInCaseId1`) and then query it—even though the column doesn’t exist in the DB.

3. **`ApplicationUserId` vs `UserId` naming**
   `CreditNote` has `public string UserId { get; set; }` and a navigation `public virtual ApplicationUser User { get; set; }`. By convention EF might try to use `ApplicationUserId` unless you **bind the nav to `UserId`** explicitly (attribute or Fluent API). That explains the missing `ApplicationUserId` column.

4. **Two user navs on `TradeIn`**
   `TradeIn` has:

* `CustomerId` + `Customer` (OK if mapped)
* `ApprovedBy` (string FK) + `ApprovedByUser` (nav)
  If not explicitly mapped, EF can introduce another shadow FK.

---

## 2) Target state (definition of done)

* App connects to the **intended DB**.
* All columns exist in SQL via EF **migrations** (no manual drift).
* **No shadow FKs**; there is only:

  * `CreditNote.TradeInCaseId` → `TradeInCase`
  * `CreditNote.UserId` → `ApplicationUser`
  * `CreditNote.TradeInId` ↔ **one-to-one** with `TradeIn.CreditNote`
  * `TradeIn.CustomerId` → `ApplicationUser`
  * `TradeIn.ApprovedBy` → `ApplicationUser` (optional)
  * `StockItem.SourceTradeInId` → `TradeIn`
* `/TradeIn` renders and all flows work.
* Startup (dev) auto-migrates and logs the connection string.

---

## 3) Implementation steps (Trae: follow in order)

### A) Log the real connection string (avoid wrong DB)

**Edit `Program.cs`** (after building configuration, before `app.Run()`):

```csharp
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Logging.AddConsole();
Console.WriteLine($"[BOOT] Using DB: {cs}");
```

> Ensure `ASPNETCORE_ENVIRONMENT` matches the appsettings you expect.

---

### B) Add explicit Fluent mappings to kill shadow FKs

**Edit `ApplicationDbContext.OnModelCreating(ModelBuilder modelBuilder)`** and add the block below (keep `base.OnModelCreating(modelBuilder);`):

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ===== TradeIn ↔ Users =====
    modelBuilder.Entity<TradeIn>(b =>
    {
        // Customer (required)
        b.HasOne(t => t.Customer)
         .WithMany() // no back-collection on ApplicationUser
         .HasForeignKey(t => t.CustomerId)
         .OnDelete(DeleteBehavior.Restrict);

        // ApprovedByUser (optional)
        b.HasOne(t => t.ApprovedByUser)
         .WithMany()
         .HasForeignKey(t => t.ApprovedBy)
         .OnDelete(DeleteBehavior.Restrict);

        // One-to-one TradeIn ↔ CreditNote (optional on TradeIn)
        b.HasOne(t => t.CreditNote)
         .WithOne() // CreditNote has no nav back to TradeIn; we bind via FK below in CreditNote
         .HasForeignKey<CreditNote>(cn => cn.TradeInId)
         .OnDelete(DeleteBehavior.Restrict);

        // Concurrency
        b.Property(t => t.RowVersion).IsRowVersion();
    });

    // ===== CreditNote mappings =====
    modelBuilder.Entity<CreditNote>(b =>
    {
        // Bind User navigation to UserId (prevents shadow "ApplicationUserId")
        b.HasOne(cn => cn.User)
         .WithMany()
         .HasForeignKey(cn => cn.UserId)
         .OnDelete(DeleteBehavior.Restrict);

        // Make the intended single FK to TradeInCase explicit (prevents TradeInCaseId1)
        b.HasOne(cn => cn.TradeInCase)
         .WithMany(tc => tc.CreditNotes)
         .HasForeignKey(cn => cn.TradeInCaseId)
         .OnDelete(DeleteBehavior.Restrict);

        // If you want the one-to-one to be enforced from CreditNote side too:
        b.HasIndex(cn => cn.TradeInId).IsUnique(); // each trade-in issues at most one credit note

        b.Property(cn => cn.RowVersion).IsRowVersion();
    });

    // ===== StockItem → TradeIn (source) =====
    modelBuilder.Entity<StockItem>(b =>
    {
        b.HasOne(si => si.SourceTradeIn)
         .WithMany(t => t.StockItems)
         .HasForeignKey(si => si.SourceTradeInId)
         .OnDelete(DeleteBehavior.Restrict);
    });

    // ===== TradeInCase graph =====
    modelBuilder.Entity<TradeInCase>(b =>
    {
        b.HasMany(tc => tc.CreditNotes)
         .WithOne(cn => cn.TradeInCase)
         .HasForeignKey(cn => cn.TradeInCaseId)
         .OnDelete(DeleteBehavior.Restrict);
    });

    // Optional: decimal precision guards (if not already decorated)
    modelBuilder.Entity<TradeIn>(b =>
    {
        b.Property(p => p.ProposedValue).HasColumnType("decimal(18,2)");
        b.Property(p => p.ApprovedValue).HasColumnType("decimal(18,2)");
    });
}
```

> This explicitly pairs every navigation with its FK and marks `RowVersion` properly, removing EF’s need to invent `…Id1` or `ApplicationUserId`.

---

### C) Ensure attributes align (no surprises)

* Keep `[Timestamp]` on `TradeIn.RowVersion` and `CreditNote.RowVersion`.
  If you prefer attributes, also add:

  ```csharp
  [ForeignKey(nameof(UserId))]
  public virtual ApplicationUser User { get; set; } = null!;
  ```

  …but the Fluent mapping already handles this.

---

### D) Create a corrective migration & update DB

```bash
dotnet tool restore
dotnet ef migrations add Fix_TradeIn_CreditNote_Mappings
dotnet ef database update
```

> If this DB is only dev data and is badly drifted, you can reset:
>
> ```
> dotnet ef database drop -f
> dotnet ef database update
> ```

---

### E) Quick SQL sanity checks (run against the same DB from step A)

```sql
-- Verify CreditNotes columns exist (no ...Id1)
EXEC sp_help 'dbo.CreditNotes';

-- Check stray shadow columns are gone
SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CreditNotes');

-- Spot check TradeIns
EXEC sp_help 'dbo.TradeIns';
```

You should **not** see `TradeInCaseId1` or `ApplicationUserId`.
You **should** see: `TradeInId`, `AmountRemaining`, `RedeemedAt`, `RedeemedOrderId`, `RowVersion`, and so on.

---

### F) Add defensive logging around the failing query

In your `TradeInService.GetUserTradeInsAsync(string userId)`:

```csharp
_logger.LogInformation("TradeIn:GetUserTradeInsAsync user={UserId}", userId);
try
{
    var q = _context.TradeIns
        .Where(t => t.CustomerId == userId)
        .Include(t => t.CreditNote); // if displayed
    var list = await q.ToListAsync();
    _logger.LogInformation("TradeIn:Found {Count} trade-ins for user={UserId}", list.Count, userId);
    return list;
}
catch (Exception ex)
{
    _logger.LogError(ex, "TradeIn:Query failed for user={UserId}", userId);
    throw;
}
```

---

### G) Dev auto-migrate & guardrails (optional but recommended)

In `Program.cs`, just before `app.Run()`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        db.Database.Migrate();
    }
}
```

This prevents “works on my machine” drift in dev.

---

## 4) Verification / Tests (Trae runbook)

1. **Start the app** → console must print the **intended** connection string.
2. Login (TradeIn requires `[Authorize]`), then visit `/TradeIn`.

   * **Expect:** page renders, no SQL exceptions.
3. Create a new Trade-In (if your UI supports it).

   * Confirm it saves and shows up on index.
4. (If flow exists) Accept Trade-In → confirm a `CreditNote` is created:

   * SQL: `SELECT TOP 5 * FROM dbo.CreditNotes ORDER BY Id DESC;`
   * Page shows the issued credit note code / amount remaining.
5. Refresh `/TradeIn` and open Details—ensure all related info renders without errors.
6. Check logs: should show “Found N trade-ins”, **no** stack traces.

**Pass criteria**

* No `Invalid column name …` anywhere.
* `dbo.CreditNotes` has **no** `TradeInCaseId1`, **no** `ApplicationUserId`.
* All columns from the earlier error exist in the DB.

---

## 5) If something still breaks — fast triage

| Symptom                       | Likely cause              | Action                                                            |
| ----------------------------- | ------------------------- | ----------------------------------------------------------------- |
| Same missing columns          | Wrong DB connected        | Re-do Step A; compare `__EFMigrationsHistory`; re-run update      |
| Only `TradeInCaseId1` missing | Mapping not applied       | Recheck Fluent mapping block, regenerate migration                |
| Missing `ApplicationUserId`   | Nav not bound to `UserId` | Ensure Fluent mapping `HasForeignKey(cn => cn.UserId)` present    |
| New column error              | Another entity drift      | Repeat migration check and add explicit mapping for that relation |

---

## 6) Notes specific to your classes

* `CreditNote.TradeInId` is **[Required]** and there’s a `TradeIn.CreditNote` nav. We enforce a **1-to-1** with `HasForeignKey<CreditNote>(cn => cn.TradeInId)` and a unique index on `TradeInId`.
* `TradeIn.RowVersion` and `CreditNote.RowVersion` should be **rowversion**/concurrency tokens. We set `.IsRowVersion()` to match the DB type.
* `TradeIn.CustomerId` and `TradeIn.ApprovedBy` both point to `ApplicationUser`. Without explicit mapping EF can invent a shadow FK—now prevented.
* `StockItem.SourceTradeInId` → explicit FK mapping avoids unexpected cascades.

