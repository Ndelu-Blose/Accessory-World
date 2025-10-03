-- Create pricing catalog tables for device assessment and pricing

-- DeviceModelCatalogs table
IF OBJECT_ID('dbo.DeviceModelCatalogs','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceModelCatalogs(
    Id int IDENTITY(1,1) PRIMARY KEY,
    Brand nvarchar(64) NOT NULL,
    Model nvarchar(128) NOT NULL,
    DeviceType nvarchar(32) NOT NULL,
    ReleaseYear int NOT NULL,
    StorageGb int NULL
  );
  CREATE UNIQUE INDEX IX_DeviceModelCatalogs_Brand_Model_Type
    ON dbo.DeviceModelCatalogs(Brand, Model, DeviceType);
END;

-- DeviceBasePrices table
IF OBJECT_ID('dbo.DeviceBasePrices','U') IS NULL
BEGIN
  CREATE TABLE dbo.DeviceBasePrices(
    Id int IDENTITY(1,1) PRIMARY KEY,
    DeviceModelCatalogId int NOT NULL
      FOREIGN KEY REFERENCES dbo.DeviceModelCatalogs(Id) ON DELETE CASCADE,
    BasePrice decimal(18,2) NOT NULL,
    AsOf datetime2 NOT NULL DEFAULT(sysutcdatetime())
  );
END;

-- PriceAdjustmentRules table
IF OBJECT_ID('dbo.PriceAdjustmentRules','U') IS NULL
BEGIN
  CREATE TABLE dbo.PriceAdjustmentRules(
    Id int IDENTITY(1,1) PRIMARY KEY,
    Code nvarchar(64) NOT NULL,
    Multiplier decimal(9,4) NOT NULL,
    FlatDeduction decimal(18,2) NULL,
    AppliesTo nvarchar(32) NOT NULL DEFAULT('ANY')
  );
END;

-- Seed test data for iPhone 13
IF NOT EXISTS (SELECT 1 FROM dbo.DeviceModelCatalogs WHERE Brand='Apple' AND Model='iphone 13' AND DeviceType='Smartphone')
BEGIN
  INSERT dbo.DeviceModelCatalogs (Brand, Model, DeviceType, ReleaseYear, StorageGb)
  VALUES ('Apple','iphone 13','Smartphone',2021,128);

  DECLARE @id int = SCOPE_IDENTITY();
  INSERT dbo.DeviceBasePrices (DeviceModelCatalogId, BasePrice) VALUES (@id, 12000.00);
END;

-- Add some basic price adjustment rules
IF NOT EXISTS (SELECT 1 FROM dbo.PriceAdjustmentRules WHERE Code='SCREEN_CRACK')
BEGIN
  INSERT dbo.PriceAdjustmentRules (Code, Multiplier, FlatDeduction, AppliesTo)
  VALUES 
    ('SCREEN_CRACK', 0.7, 0, 'ANY'),
    ('BODY_DAMAGE', 0.8, 0, 'ANY'),
    ('WATER_DAMAGE', 0.5, 0, 'ANY'),
    ('EXCELLENT_CONDITION', 1.0, 0, 'ANY'),
    ('GOOD_CONDITION', 0.85, 0, 'ANY'),
    ('FAIR_CONDITION', 0.65, 0, 'ANY'),
    ('POOR_CONDITION', 0.4, 0, 'ANY');
END;