# 1) Database Fix — pricing catalog + safe seeds

You saw errors like **`Invalid object name 'DeviceModelCatalogs'`**. Fix it using **Path A (SQL)** or **Path B (EF)**. Do **one** path only.

## Path A — Raw SQL (quickest)

Run against the same SQL DB your app uses:

```sql
/* ===== Device model catalog ===== */
IF OBJECT_ID('dbo.DeviceModelCatalogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceModelCatalogs(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Brand nvarchar(64) NOT NULL,
        Model nvarchar(128) NOT NULL,
        DeviceType nvarchar(32) NOT NULL,
        ReleaseYear int NOT NULL,
        StorageGb int NULL
    );
    CREATE UNIQUE INDEX IX_DeviceModelCatalogs_Brand_Model_Type
      ON dbo.DeviceModelCatalogs(Brand, Model, DeviceType);
END;

/* ===== Base prices (FK to catalog) ===== */
IF OBJECT_ID('dbo.DeviceBasePrices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceBasePrices(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DeviceModelCatalogId int NOT NULL
            FOREIGN KEY REFERENCES dbo.DeviceModelCatalogs(Id) ON DELETE CASCADE,
        BasePrice decimal(18,2) NOT NULL,
        AsOf datetime2 NOT NULL DEFAULT(sysutcdatetime())
    );
END;

/* ===== Price adjustment rules ===== */
IF OBJECT_ID('dbo.PriceAdjustmentRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceAdjustmentRules(
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code nvarchar(64) NOT NULL,
        Multiplier decimal(9,4) NOT NULL,   -- e.g. 0.8500
        FlatDeduction decimal(18,2) NULL,
        AppliesTo nvarchar(32) NOT NULL DEFAULT('ANY')
    );
END;

/* ===== Safe seed for iPhone 13 (so quotes don’t fail) ===== */
IF NOT EXISTS (
    SELECT 1 FROM dbo.DeviceModelCatalogs
    WHERE Brand='Apple' AND Model='iPhone 13' AND DeviceType='Smartphone'
)
BEGIN
    INSERT INTO dbo.DeviceModelCatalogs (Brand, Model, DeviceType, ReleaseYear, StorageGb)
    VALUES ('Apple', 'iPhone 13', 'Smartphone', 2021, 128);

    DECLARE @id int = SCOPE_IDENTITY();
    INSERT INTO dbo.DeviceBasePrices (DeviceModelCatalogId, BasePrice)
    VALUES (@id, 12000.00);
END
```

## Path B — EF migration (if you prefer EF CLI)

1. Ensure the **connection string** matches your running environment.
2. Add migration:
   ```bash
   dotnet ef migrations add PricingCatalog_Initial
   ```
3. Paste the `Up`/`Down` from the SQL definitions above into migration code (table types/columns/indices must match).
4. Apply:
   ```bash
   dotnet ef database update
   ```

> After this, `SELECT TOP 1 * FROM DeviceModelCatalogs` should return at least one row.
