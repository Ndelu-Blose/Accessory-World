Got it — the crash is a straight SQL error:

**SqlException: “Invalid object name 'TradeIns'.”**

That means SQL Server can’t find a table called `TradeIns`. Nothing else in the stack matters until this is fixed. Here’s the quickest way to resolve it, in order.

# 1) Confirm you’re hitting the right database

It’s very common to point your app at one DB, run migrations on another, then query a table that doesn’t exist.

* Print/log the **actual connection string** your app is using at startup (mask password).
* In SSMS, connect to that exact DB and run:

  ```sql
  SELECT DB_NAME() AS CurrentDB;
  SELECT * FROM sys.tables WHERE name IN ('TradeIns','TradeIn');
  SELECT * FROM sys.schemas;
  SELECT * FROM __EFMigrationsHistory;
  ```

  If `TradeIns` isn’t listed, the table truly doesn’t exist in that DB.

# 2) Fix the table name vs model mapping

EF will query the table name you mapped. Right now it’s trying `TradeIns` (plural). If your existing table is singular (`TradeIn`) or lives in a non-dbo schema, you’ll get this error.

Pick **one** approach and make your code match it.

### A) You want the table to be `TradeIns` (plural)

* Ensure your entity maps to `TradeIns`:

  ```csharp
  // In DbContext.OnModelCreating
  modelBuilder.Entity<TradeIn>().ToTable("TradeIns"); // add second arg for schema if needed
  ```
* If the table doesn’t exist, create it via migration:

  ```bash
  dotnet ef migrations add CreateTradeIns
  dotnet ef database update
  ```

  (PMC: `Add-Migration CreateTradeIns` then `Update-Database`)

### B) Your DB already has `TradeIn` (singular)

* Map to the existing name to avoid touching data:

  ```csharp
  modelBuilder.Entity<TradeIn>().ToTable("TradeIn");
  ```
* Add a small migration (no table creation) if EF needs the rename recorded. Or leave it if you’re only changing the mapping.

### C) Your table lives in a custom schema (e.g., `aw.TradeIns`)

* Map the schema explicitly:

  ```csharp
  modelBuilder.HasDefaultSchema("aw"); // optional if all tables use 'aw'
  modelBuilder.Entity<TradeIn>().ToTable("TradeIns","aw");
  ```
* Or move the table to `dbo` or update your migration accordingly.

# 3) Make sure migrations actually ran on this DB

If `__EFMigrationsHistory` is empty (or missing your latest migration), your schema hasn’t been applied.

Run:

```bash
dotnet ef database update
```

If you’re using multiple connection profiles (Development/Production), verify the **environment** and **connection string** used by the command match the app.

# 4) Avoid “EnsureCreated” with migrations

If you have code like:

```csharp
context.Database.EnsureCreated();
```

remove it when using migrations. It can create a partial schema without `__EFMigrationsHistory`, then future queries/migrations don’t line up. Use only `dotnet ef database update`.

# 5) Sanity checks in your code

* Ensure you actually have `DbSet<TradeIn> TradeIns { get; set; }` on your DbContext.
* If you renamed the entity or moved it to another assembly, confirm the context still scans it (e.g., same project, or apply configuration via `modelBuilder.ApplyConfigurationsFromAssembly(...)`).

# 6) If you need a quick “works-now” patch (not recommended long-term)

As a temporary local fix (to unblock testing), map to whatever table name exists right now:

```csharp
modelBuilder.Entity<TradeIn>().ToTable("TradeIn"); // or "TradeIns"
```

…but do follow up with a proper migration to keep code and DB aligned.

---

## What I’d do next on your project

1. Open SSMS on the **same DB** your app uses; verify whether `dbo.TradeIns` exists.
2. If it doesn’t, run a migration to create it.
3. If a differently named table exists, update the EF mapping to match it.
4. Re-run and the `/TradeIn` page should load without the “Invalid object name” crash.




 Your crash (“Invalid object name ‘TradeIns’”) is because EF is querying a table named **TradeIns**, but in most EF Core setups the default table (unless you override it) is **TradeIn** (singular). So your DB most likely has `dbo.TradeIn`, while the model is resolving to `TradeIns`.

### Fastest fix (map explicitly to the existing table)

Add a `.ToTable("TradeIn")` for the `TradeIn` entity so EF stops asking for `TradeIns`.

```csharp
// Configure TradeIn
modelBuilder.Entity<TradeIn>(entity =>
{
    entity.ToTable("TradeIn", "dbo"); // 👈 explicit table (and schema if dbo)
    entity.HasKey(e => e.Id);
    entity.Property(e => e.PublicId).HasDefaultValueSql("NEWID()");
    entity.Property(e => e.CustomerId).IsRequired();
    entity.Property(e => e.DeviceBrand).IsRequired().HasMaxLength(64).HasDefaultValue("Apple");
    entity.Property(e => e.DeviceModel).IsRequired().HasMaxLength(128);
    entity.Property(e => e.IMEI).HasMaxLength(32);
    entity.Property(e => e.ConditionGrade).IsRequired().HasMaxLength(2);
    entity.Property(e => e.PhotosJson).IsRequired().HasColumnType("nvarchar(max)");
    entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
    entity.Property(e => e.ProposedValue).HasColumnType("decimal(18,2)");
    entity.Property(e => e.ApprovedValue).HasColumnType("decimal(18,2)");
    entity.Property(e => e.Notes).HasColumnType("nvarchar(max)");
    entity.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("SYSUTCDATETIME()");
    entity.Property(e => e.RowVersion).IsRowVersion();

    entity.HasIndex(e => e.PublicId).IsUnique();
    entity.HasIndex(e => new { e.CustomerId, e.Status });

    entity.ToTable(t =>
        t.HasCheckConstraint("CK_TradeIn_ConditionGrade", "[ConditionGrade] IN ('A','B','C','D')")
    );

    entity.HasOne(e => e.Customer)
        .WithMany()
        .HasForeignKey(e => e.CustomerId)
        .OnDelete(DeleteBehavior.Restrict);

    entity.Property(e => e.ApprovedBy).HasMaxLength(450);

    entity.HasOne(e => e.ApprovedByUser)
        .WithMany()
        .HasForeignKey(e => e.ApprovedBy)
        .OnDelete(DeleteBehavior.Restrict);
});
```

### Then do one of these:

* If the **singular** table already exists (likely), you can just run the app after this change.
* If there’s **no** table yet, create it with migrations:

  ```bash
  dotnet ef migrations add CreateTradeInTable
  dotnet ef database update
  ```

### Double-check you’re on the right DB

Just to be sure you’re not pointed at an empty database, in SSMS on the **same connection string** the app uses:

```sql
SELECT DB_NAME() AS CurrentDB;
SELECT name FROM sys.tables WHERE name IN ('TradeIn','TradeIns');
SELECT * FROM __EFMigrationsHistory;
```

### Why this happened

* EF Core’s default table name is the **entity type name** (singular).
* Something in your setup (a pluralizing convention, or an old migration) made EF expect `TradeIns`. Mapping explicitly removes any ambiguity and aligns code with the actual table.

If you actually want pluralized names everywhere, we can flip the approach: keep `.ToTable("TradeIns")` and add/run a migration that **renames** `TradeIn` → `TradeIns`. But the one-line mapping above is the quickest unblocking fix.
