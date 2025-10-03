Got it—let’s stop the loop and pin this down. Your logs still show SQL Server saying those columns don’t exist, which means the app is querying a database whose **actual tables** don’t match your model. That’s the core issue.

Here’s a short, surgical plan that will tell us exactly what’s wrong and fix it—even if migrations have gotten messy.

# 1) Confirm the **exact** DB the app is hitting

You already print this at startup. Copy that connection string and run the 3 queries below against that **same** server and database (SSMS/ADS/`sqlcmd`).

```sql
-- A. sanity: which DB am I in?
SELECT DB_NAME() AS DbName;

-- B. which columns does SQL Server actually see?
SELECT 
  COL_LENGTH('dbo.TradeIns','DeviceType')               AS DeviceType_,
  COL_LENGTH('dbo.TradeIns','Description')              AS Description_,
  COL_LENGTH('dbo.TradeIns','AiVendor')                 AS AiVendor_,
  COL_LENGTH('dbo.TradeIns','AiVersion')                AS AiVersion_,
  COL_LENGTH('dbo.TradeIns','AiAssessmentJson')         AS AiAssessmentJson_,
  COL_LENGTH('dbo.TradeIns','AiConfidence')             AS AiConfidence_,
  COL_LENGTH('dbo.TradeIns','AutoGrade')                AS AutoGrade_,
  COL_LENGTH('dbo.TradeIns','AutoOfferAmount')          AS AutoOfferAmount_,
  COL_LENGTH('dbo.TradeIns','AutoOfferBreakdownJson')   AS AutoOfferBreakdownJson_,
  COL_LENGTH('dbo.TradeIns','AiRetryCount')             AS AiRetryCount_,
  COL_LENGTH('dbo.TradeIns','RowVersion')               AS RowVersion_;

SELECT 
  COL_LENGTH('dbo.CreditNotes','TradeInId')             AS CreditNotes_TradeInId_,
  COL_LENGTH('dbo.CreditNotes','AmountRemaining')       AS AmountRemaining_,
  COL_LENGTH('dbo.CreditNotes','NonWithdrawable')       AS NonWithdrawable_,
  COL_LENGTH('dbo.CreditNotes','RedeemedAt')            AS RedeemedAt_,
  COL_LENGTH('dbo.CreditNotes','RedeemedOrderId')       AS RedeemedOrderId_,
  COL_LENGTH('dbo.CreditNotes','StoreCreditOnly')       AS StoreCreditOnly_,
  COL_LENGTH('dbo.CreditNotes','RowVersion')            AS CN_RowVersion_;

-- C. what EF migrations has this DB seen?
SELECT * FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
```

* Any value that returns **NULL** is missing in the DB.
* If `__EFMigrationsHistory` is empty or missing the migrations you expected, you’ve been applying migrations to a *different* database.

# 2) Patch the live schema (idempotent, safe to run)

If the checks in step 1 show NULLs, run this once on that same DB:

```sql
-- TRADEINS
IF COL_LENGTH('dbo.TradeIns','DeviceType') IS NULL
  ALTER TABLE dbo.TradeIns ADD DeviceType nvarchar(64) NULL;
IF COL_LENGTH('dbo.TradeIns','Description') IS NULL
  ALTER TABLE dbo.TradeIns ADD Description nvarchar(max) NULL;
IF COL_LENGTH('dbo.TradeIns','AiVendor') IS NULL
  ALTER TABLE dbo.TradeIns ADD AiVendor nvarchar(50) NULL;
IF COL_LENGTH('dbo.TradeIns','AiVersion') IS NULL
  ALTER TABLE dbo.TradeIns ADD AiVersion nvarchar(50) NULL;
IF COL_LENGTH('dbo.TradeIns','AiAssessmentJson') IS NULL
  ALTER TABLE dbo.TradeIns ADD AiAssessmentJson nvarchar(max) NULL;
IF COL_LENGTH('dbo.TradeIns','AiConfidence') IS NULL
  ALTER TABLE dbo.TradeIns ADD AiConfidence float NULL;
IF COL_LENGTH('dbo.TradeIns','AutoGrade') IS NULL
  ALTER TABLE dbo.TradeIns ADD AutoGrade nvarchar(5) NULL;
IF COL_LENGTH('dbo.TradeIns','AutoOfferAmount') IS NULL
  ALTER TABLE dbo.TradeIns ADD AutoOfferAmount decimal(18,2) NULL;
IF COL_LENGTH('dbo.TradeIns','AutoOfferBreakdownJson') IS NULL
  ALTER TABLE dbo.TradeIns ADD AutoOfferBreakdownJson nvarchar(max) NULL;
IF COL_LENGTH('dbo.TradeIns','AiRetryCount') IS NULL
  ALTER TABLE dbo.TradeIns ADD AiRetryCount int NULL;
IF COL_LENGTH('dbo.TradeIns','RowVersion') IS NULL
  ALTER TABLE dbo.TradeIns ADD RowVersion rowversion;

-- CREDITNOTES
IF COL_LENGTH('dbo.CreditNotes','TradeInId') IS NULL
  ALTER TABLE dbo.CreditNotes ADD TradeInId int NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_CreditNotes_TradeIns_TradeInId')
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD CONSTRAINT FK_CreditNotes_TradeIns_TradeInId
    FOREIGN KEY (TradeInId) REFERENCES dbo.TradeIns(Id) ON DELETE CASCADE;
END

IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL
  ALTER TABLE dbo.CreditNotes ADD AmountRemaining decimal(18,2) NOT NULL CONSTRAINT DF_CN_AmountRemaining DEFAULT(0);

IF COL_LENGTH('dbo.CreditNotes','NonWithdrawable') IS NULL
  ALTER TABLE dbo.CreditNotes ADD NonWithdrawable bit NOT NULL CONSTRAINT DF_CN_NonWithdrawable DEFAULT(0);

IF COL_LENGTH('dbo.CreditNotes','RedeemedAt') IS NULL
  ALTER TABLE dbo.CreditNotes ADD RedeemedAt datetime2 NULL;

IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NULL
  ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId int NULL;

IF COL_LENGTH('dbo.CreditNotes','StoreCreditOnly') IS NULL
  ALTER TABLE dbo.CreditNotes ADD StoreCreditOnly bit NOT NULL CONSTRAINT DF_CN_StoreCreditOnly DEFAULT(0);

IF COL_LENGTH('dbo.CreditNotes','RowVersion') IS NULL
  ALTER TABLE dbo.CreditNotes ADD RowVersion rowversion;
```

This directly aligns the physical schema with the properties your logs complain about. It’s **idempotent**: if a column already exists, nothing happens.

# 3) Make the relationship explicit (prevents EF from creating shadow FKs)

In your model, add the back-reference on `CreditNote` and (optionally) map it in `OnModelCreating`:

```csharp
// in CreditNote
[ForeignKey(nameof(TradeInId))]
public virtual TradeIn TradeIn { get; set; } = null!;
```

```csharp
// in ApplicationDbContext.OnModelCreating
modelBuilder.Entity<TradeIn>()
  .HasOne(t => t.CreditNote)
  .WithOne(cn => cn.TradeIn)
  .HasForeignKey<CreditNote>(cn => cn.TradeInId)
  .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<TradeIn>()
  .Property(t => t.RowVersion)
  .IsRowVersion();

modelBuilder.Entity<CreditNote>()
  .Property(cn => cn.RowVersion)
  .IsRowVersion();
```

This eliminates “shadow” properties and ensures EF always uses `CreditNotes.TradeInId` for the join you see in the logs.

# 4) Keep EF and DB in sync going forward

Generate an idempotent script (great for any environment to catch up safely):

```bash
dotnet ef migrations script --idempotent -o ef-sync.sql
```

Run `ef-sync.sql` against the same DB from step 1.

---

If you want me to pinpoint exactly what’s still missing, paste back just these:

* The **three query results** from step 1 (DbName, the two `COL_LENGTH` result sets, and `__EFMigrationsHistory`).
* The startup **connection string** the app prints.

From there, I’ll tell you precisely which columns/FKs are absent on the database you’re actually hitting and provide the minimal fix.
