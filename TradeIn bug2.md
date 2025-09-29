Got it 🚀 — here’s a **Trae-AI ready execution pack** written in a step-by-step, **do → check → confirm** style so Trae won’t get confused. Each step tells Trae exactly **what to change**, **what to run**, and **what success looks like**.

---

# TRAE EXECUTION PACK — Fix TradeIn / CreditNote Errors

---

## 1. Fix `TradeInCaseId1` Shadow Foreign Key

### Action

Open **`ApplicationDbContext.cs`**, inside `OnModelCreating`.
Find the **CreditNote** configuration.

Replace this block:

```csharp
entity.HasOne(e => e.TradeInCase)
      .WithMany()
      .HasForeignKey(e => e.TradeInCaseId)
      .OnDelete(DeleteBehavior.SetNull);
```

with:

```csharp
entity.HasOne(e => e.TradeInCase)
      .WithMany(tc => tc.CreditNotes)   // bind to the ICollection in TradeInCase
      .HasForeignKey(e => e.TradeInCaseId)
      .OnDelete(DeleteBehavior.SetNull);
```

### Why

This binds **both sides of the relationship** (`TradeInCase ↔ CreditNote`).
EF will no longer generate `TradeInCaseId1`.

### Success Check

* Restart app.
* EF **warnings about `TradeInCaseId1` disappear** in console.

---

## 2. Fix `ApplicationUserId` Shadow Foreign Key

### Action

In `OnModelCreating`, search for any **duplicate mapping of User → CreditNote**.
Currently you have this **twice**:

```csharp
// once in the “User -> CreditNote” block
modelBuilder.Entity<CreditNote>()
    .HasOne(cn => cn.User)
    .WithMany(u => u.CreditNotes)
    .HasForeignKey(cn => cn.UserId)
    .OnDelete(DeleteBehavior.Restrict);

// and again inside the CreditNote config
entity.HasOne(e => e.User)
      .WithMany()
      .HasForeignKey(e => e.UserId)
      .OnDelete(DeleteBehavior.Restrict);
```

### Fix

**Delete the first one** (the global one).
Keep only the mapping inside the **CreditNote config**.

Change that one to:

```csharp
entity.HasOne(e => e.User)
      .WithMany(u => u.CreditNotes)   // bind to the ICollection in ApplicationUser
      .HasForeignKey(e => e.UserId)
      .OnDelete(DeleteBehavior.Restrict);
```

### Why

This ensures EF only uses `UserId`.
EF will stop expecting a phantom `ApplicationUserId` column.

### Success Check

* Restart app.
* EF warnings about `ApplicationUserId` **disappear**.

---

## 3. Add Missing Columns to Database

### Action

Run in terminal:

```bash
dotnet ef migrations add Patch_CreditNote_Columns
```

Open the generated migration file.
Replace the `Up` method with:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.CreditNotes','TradeInId') IS NULL
    ALTER TABLE dbo.CreditNotes ADD TradeInId INT NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL
    ALTER TABLE dbo.CreditNotes ADD AmountRemaining DECIMAL(18,2) NOT NULL DEFAULT 0;

IF COL_LENGTH('dbo.CreditNotes','RedeemedAt') IS NULL
    ALTER TABLE dbo.CreditNotes ADD RedeemedAt DATETIME2 NULL;

IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NULL
    ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId INT NULL;

IF COL_LENGTH('dbo.CreditNotes','RowVersion') IS NULL
    ALTER TABLE dbo.CreditNotes ADD RowVersion ROWVERSION;

-- Cleanup bad columns
IF COL_LENGTH('dbo.CreditNotes','ApplicationUserId') IS NOT NULL
    ALTER TABLE dbo.CreditNotes DROP COLUMN ApplicationUserId;

IF COL_LENGTH('dbo.CreditNotes','TradeInCaseId1') IS NOT NULL
    ALTER TABLE dbo.CreditNotes DROP COLUMN TradeInCaseId1;
");
}
```

Then apply:

```bash
dotnet ef database update
```

### Why

Adds the missing columns that your model requires, and drops the stray shadow FKs.

### Success Check

Run in SQL Server:

```sql
EXEC sp_help 'dbo.CreditNotes';
```

Confirm table now has:
✅ `TradeInId`
✅ `AmountRemaining`
✅ `RedeemedAt`
✅ `RedeemedOrderId`
✅ `RowVersion`
❌ No `ApplicationUserId`
❌ No `TradeInCaseId1`

---

## 4. Test End-to-End

### Action

1. Start app.
2. Login as a user.
3. Navigate to `/TradeIn`.

### Expect

* Page loads, **no SQL error**.
* Logs show:

  ```
  TradeIn:GetUserTradeInsAsync user={id}
  TradeIn:Found N items for user={id}
  ```

---

# 🎯 Final Success Criteria

* EF **no longer logs shadow FK warnings**.
* `/TradeIn` page loads with **no invalid column errors**.
* `dbo.CreditNotes` has correct schema.
* New Trade-Ins can be created and accepted, producing valid CreditNotes.

---

**rewrite the full `CreditNote` + `TradeInCase` + `User` mapping block** exactly as it should appear in  `OnModelCreating`, 


**Do this exactly:** in `ApplicationDbContext.OnModelCreating(...)`, **remove your existing “Configure CreditNote” block** and also **remove any earlier “User -> CreditNote” mapping**. Then paste the code below anywhere inside `OnModelCreating` (after `base.OnModelCreating(modelBuilder);` is fine).

```csharp
// ===============================
// REPLACEMENT: CreditNote / TradeInCase / User / TradeIn wiring
// ===============================
modelBuilder.Entity<CreditNote>(entity =>
{
    // Keys & columns
    entity.HasKey(e => e.Id);

    entity.Property(e => e.CreditNoteCode)
          .IsRequired()
          .HasMaxLength(20);

    entity.Property(e => e.UserId)
          .IsRequired()
          .HasMaxLength(450);

    entity.Property(e => e.TradeInId)
          .IsRequired(); // FK to TradeIn (1:1)

    entity.Property(e => e.TradeInCaseId);     // nullable
    entity.Property(e => e.ConsumedInOrderId); // nullable

    entity.Property(e => e.Amount)
          .IsRequired()
          .HasColumnType("decimal(18,2)");

    entity.Property(e => e.AmountRemaining)
          .IsRequired()
          .HasColumnType("decimal(18,2)");

    entity.Property(e => e.Status)
          .IsRequired()
          .HasMaxLength(32);

    entity.Property(e => e.ExpiresAt)
          .IsRequired();

    entity.Property(e => e.CreatedAt)
          .IsRequired()
          .HasDefaultValueSql("SYSUTCDATETIME()");

    entity.Property(e => e.RedeemedAt);     // nullable
    entity.Property(e => e.RedeemedOrderId);// nullable

    entity.Property(e => e.RowVersion).IsRowVersion();

    // Indexes
    entity.HasIndex(e => e.CreditNoteCode).IsUnique();
    entity.HasIndex(e => new { e.UserId, e.Status });
    // One CreditNote per TradeIn
    entity.HasIndex(e => e.TradeInId).IsUnique();

    // Relationships

    // Bind to ApplicationUser via UserId (prevents shadow 'ApplicationUserId')
    entity.HasOne(e => e.User)
          .WithMany(u => u.CreditNotes)              // <-- bind to the collection on ApplicationUser
          .HasForeignKey(e => e.UserId)
          .OnDelete(DeleteBehavior.Restrict);

    // Bind to TradeInCase via TradeInCaseId (prevents shadow 'TradeInCaseId1')
    entity.HasOne(e => e.TradeInCase)
          .WithMany(tc => tc.CreditNotes)            // <-- bind to the collection on TradeInCase
          .HasForeignKey(e => e.TradeInCaseId)
          .OnDelete(DeleteBehavior.SetNull);

    // Optional: the order in which a credit note was consumed
    entity.HasOne(e => e.ConsumedInOrder)
          .WithMany()                                // no back-collection on Order
          .HasForeignKey(e => e.ConsumedInOrderId)
          .OnDelete(DeleteBehavior.SetNull);
});

// Ensure the 1:1 between TradeIn and CreditNote is explicit
modelBuilder.Entity<TradeIn>(entity =>
{
    entity.HasOne(t => t.CreditNote)
          .WithOne()                                 // CreditNote has no nav back to TradeIn
          .HasForeignKey<CreditNote>(cn => cn.TradeInId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

### After pasting:

1. **Build** the project (ensures the model compiles).
2. Create a **targeted** patch migration and apply it:

```bash
dotnet ef migrations add Patch_CreditNote_Columns
dotnet ef database update
```

3. **Quick DB check** (run in SSMS):

```sql
EXEC sp_help 'dbo.CreditNotes';
```

You should now **have**: `TradeInId`, `AmountRemaining`, `RedeemedAt`, `RedeemedOrderId`, `RowVersion`
You should **not** have: `ApplicationUserId`, `TradeInCaseId1`

4. Run the app, login, open `/TradeIn` — it should load without the invalid-column errors.

Phase 2
TRAE FIX SCRIPT — bring DB in line with the model
A) Database patch (run exactly these T-SQL snippets)

Run against your real DB: AccessoryWorldDb on (localdb)\MSSQLLocalDB.

1) Fix TradeInId type (uniqueidentifier → int)
-- 1. Drop index that references TradeInId if present (you created it earlier)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CreditNotes_TradeInId' AND object_id = OBJECT_ID('dbo.CreditNotes'))
    DROP INDEX IX_CreditNotes_TradeInId ON dbo.CreditNotes;

-- 2. If TradeInId exists as UNIQUEIDENTIFIER, drop and re-add as INT (NOT NULL, default 0)
IF COL_LENGTH('dbo.CreditNotes','TradeInId') IS NOT NULL
BEGIN
    DECLARE @type NVARCHAR(128) =
        (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNotes' AND COLUMN_NAME='TradeInId');
    IF (@type = 'uniqueidentifier')
    BEGIN
        ALTER TABLE dbo.CreditNotes DROP COLUMN TradeInId;
    END
END

IF COL_LENGTH('dbo.CreditNotes','TradeInId') IS NULL
    ALTER TABLE dbo.CreditNotes ADD TradeInId INT NOT NULL CONSTRAINT DF_CreditNotes_TradeInId DEFAULT (0);

-- 3. Recreate unique index if you enforce one CreditNote per TradeIn (optional)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CreditNotes_TradeInId' AND object_id = OBJECT_ID('dbo.CreditNotes'))
    CREATE UNIQUE INDEX IX_CreditNotes_TradeInId ON dbo.CreditNotes (TradeInId);

2) Rename RemainingAmount → AmountRemaining
IF COL_LENGTH('dbo.CreditNotes','RemainingAmount') IS NOT NULL
AND COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL
    EXEC sp_rename 'dbo.CreditNotes.RemainingAmount', 'AmountRemaining', 'COLUMN';

-- ensure the type/scale is correct
IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NOT NULL
BEGIN
    -- make sure decimal(18,2)
    ALTER TABLE dbo.CreditNotes ALTER COLUMN AmountRemaining DECIMAL(18,2) NOT NULL;
END

3) Fix RedeemedOrderId type (uniqueidentifier → int)
IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NOT NULL
BEGIN
    DECLARE @type2 NVARCHAR(128) =
        (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNotes' AND COLUMN_NAME='RedeemedOrderId');
    IF (@type2 = 'uniqueidentifier')
    BEGIN
        ALTER TABLE dbo.CreditNotes DROP COLUMN RedeemedOrderId;
        ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId INT NULL;
    END
END
ELSE
BEGIN
    ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId INT NULL;
END

4) Add missing RowVersion
IF COL_LENGTH('dbo.CreditNotes','RowVersion') IS NULL
    ALTER TABLE dbo.CreditNotes ADD RowVersion ROWVERSION;

5) Clean up stray columns (if any remain)
IF COL_LENGTH('dbo.CreditNotes','ApplicationUserId') IS NOT NULL
    ALTER TABLE dbo.CreditNotes DROP COLUMN ApplicationUserId;

IF COL_LENGTH('dbo.CreditNotes','TradeInCaseId1') IS NOT NULL
    ALTER TABLE dbo.CreditNotes DROP COLUMN TradeInCaseId1;

6) Quick verification
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CreditNotes'
ORDER BY ORDINAL_POSITION;


You should see:

TradeInId → int (NOT NULL)

AmountRemaining → decimal (NOT NULL)

RedeemedOrderId → int (NULL)

RowVersion → timestamp/rowversion (implicit NOT NULL)

B) Model tidy to stop shadow FKs & warnings
1) Remove the duplicate SKU mapping that causes SKUId1

You currently map SKU → StockMovement twice:

globally near the top; and

again in the StockMovement block.

Remove the global one (this part near the top):

// SKU -> StockMovement (One-to-Many)  <<< REMOVE THIS BLOCK
modelBuilder.Entity<StockMovement>()
    .HasOne(sm => sm.SKU)
    .WithMany(s => s.StockMovements)
    .HasForeignKey(sm => sm.SKUId)
    .OnDelete(DeleteBehavior.Cascade);


Keep only the mapping inside the StockMovement configuration:

entity.HasOne(e => e.SKU)
      .WithMany(s => s.StockMovements)
      .HasForeignKey(e => e.SKUId)
      .OnDelete(DeleteBehavior.Restrict);


This removes the “two relationships to SKU” ambiguity that produces SKUId1.

2) (You already fixed) CreditNote ↔ TradeInCase / User

You pasted the replacement block earlier. Ensure it’s exactly this in your CreditNote config:

entity.HasOne(e => e.User)
      .WithMany(u => u.CreditNotes)
      .HasForeignKey(e => e.UserId)
      .OnDelete(DeleteBehavior.Restrict);

entity.HasOne(e => e.TradeInCase)
      .WithMany(tc => tc.CreditNotes)
      .HasForeignKey(e => e.TradeInCaseId)
      .OnDelete(DeleteBehavior.SetNull);

entity.HasOne(e => e.ConsumedInOrder)
      .WithMany()
      .HasForeignKey(e => e.ConsumedInOrderId)
      .OnDelete(DeleteBehavior.SetNull);

// One-to-one binding via TradeInId
modelBuilder.Entity<TradeIn>(t =>
{
    t.HasOne(x => x.CreditNote)
     .WithOne()
     .HasForeignKey<CreditNote>(cn => cn.TradeInId)
     .OnDelete(DeleteBehavior.Restrict);
});

3) Silence the decimal precision warnings (Settings)

Add:

modelBuilder.Entity<Settings>(b =>
{
    b.Property(x => x.ShippingCost).HasColumnType("decimal(18,2)");
    b.Property(x => x.TaxRate).HasColumnType("decimal(18,4)");
});

C) Rebuild & run — and what to expect

Rebuild the solution.

Run the app.

Login → browse to /TradeIn.

Expected:

No more Operand type clash (TradeInId is now int).

No more Invalid column name 'AmountRemaining' (renamed).

No more Invalid column name 'RowVersion' (added).

Shadow FK warnings for SKUId1 vanish (removed duplicate mapping).

Existing “TaxRate/ShippingCost” warnings gone (store types set).

Your logs should now show something like:

Retrieving trade-ins for user {id}
SELECT [t] ... LEFT JOIN [c] ON [t].[Id] = [c].[TradeInId] ...
TradeIn:Found N items for user={id}

D) Optional: make EF migrations happy again (later)

Because you manually patched the DB, your next EF migration can complain if the ModelSnapshot says something different. Once everything runs clean:

Scaffold a no-op migration to sync snapshot:

dotnet ef migrations add SyncSnapshot_AfterHotfix
dotnet ef database update


If EF tries to “recreate” things, abort and regenerate ensuring your current model matches the DB we just enforced.

If anything still errors

If you still see a type clash, re-run:

SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNotes';


and confirm TradeInId = int, RedeemedOrderId = int, AmountRemaining exists, RowVersion exists.

If you somehow still get SKUId1, grep your context for another HasOne(sm => sm.SKU) mapping you may have missed and remove the duplicate.