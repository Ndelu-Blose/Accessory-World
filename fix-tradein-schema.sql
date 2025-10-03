-- Fix Trade-In Database Schema
-- Add missing AI assessment columns to TradeIns table

-- TradeIns table fixes
IF COL_LENGTH('dbo.TradeIns','DeviceType') IS NULL 
  ALTER TABLE dbo.TradeIns ADD DeviceType nvarchar(64) NULL;

IF COL_LENGTH('dbo.TradeIns','Description') IS NULL 
  ALTER TABLE dbo.TradeIns ADD Description nvarchar(max) NULL;

IF COL_LENGTH('dbo.TradeIns','AiVendor') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AiVendor nvarchar(64) NULL;

IF COL_LENGTH('dbo.TradeIns','AiVersion') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AiVersion nvarchar(32) NULL;

IF COL_LENGTH('dbo.TradeIns','AiAssessmentJson') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AiAssessmentJson nvarchar(max) NULL;

IF COL_LENGTH('dbo.TradeIns','AiConfidence') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AiConfidence real NULL;

IF COL_LENGTH('dbo.TradeIns','AutoGrade') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AutoGrade nvarchar(2) NULL;

IF COL_LENGTH('dbo.TradeIns','AutoOfferAmount') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AutoOfferAmount decimal(18,2) NULL;

IF COL_LENGTH('dbo.TradeIns','AutoOfferBreakdownJson') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AutoOfferBreakdownJson nvarchar(max) NULL;

IF COL_LENGTH('dbo.TradeIns','AiRetryCount') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AiRetryCount int NOT NULL DEFAULT(0);

-- Add audit/lifecycle fields
IF COL_LENGTH('dbo.TradeIns','AssessedAt') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AssessedAt datetimeoffset NULL;

IF COL_LENGTH('dbo.TradeIns','UserAcceptedAt') IS NULL 
  ALTER TABLE dbo.TradeIns ADD UserAcceptedAt datetimeoffset NULL;

IF COL_LENGTH('dbo.TradeIns','AdminApprovedAt') IS NULL 
  ALTER TABLE dbo.TradeIns ADD AdminApprovedAt datetimeoffset NULL;

IF COL_LENGTH('dbo.TradeIns','CreditIssuedAt') IS NULL 
  ALTER TABLE dbo.TradeIns ADD CreditIssuedAt datetimeoffset NULL;

IF COL_LENGTH('dbo.TradeIns','CreditNoteId') IS NULL 
  ALTER TABLE dbo.TradeIns ADD CreditNoteId int NULL;

-- Fix CreatedAt to be datetimeoffset
IF COL_LENGTH('dbo.TradeIns','CreatedAt') IS NOT NULL 
BEGIN
  -- Check if it's already datetimeoffset
  IF (SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TradeIns' AND COLUMN_NAME = 'CreatedAt') = 'datetime2'
  BEGIN
    ALTER TABLE dbo.TradeIns ALTER COLUMN CreatedAt datetimeoffset NOT NULL;
  END
END

-- CreditNotes table fixes (if missing fields were causing errors)
IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL 
  ALTER TABLE dbo.CreditNotes ADD AmountRemaining decimal(18,2) NOT NULL DEFAULT(0);

IF COL_LENGTH('dbo.CreditNotes','RedeemedAt') IS NULL 
  ALTER TABLE dbo.CreditNotes ADD RedeemedAt datetimeoffset NULL;

IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NULL 
  ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId int NULL;

-- Add indexes for performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeIns_PublicId' AND object_id = OBJECT_ID('dbo.TradeIns'))
  CREATE UNIQUE INDEX IX_TradeIns_PublicId ON dbo.TradeIns (PublicId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeIns_CustomerId_Status' AND object_id = OBJECT_ID('dbo.TradeIns'))
  CREATE INDEX IX_TradeIns_CustomerId_Status ON dbo.TradeIns (CustomerId, Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeIns_AiVendor' AND object_id = OBJECT_ID('dbo.TradeIns'))
  CREATE INDEX IX_TradeIns_AiVendor ON dbo.TradeIns (AiVendor);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TradeIns_AutoGrade' AND object_id = OBJECT_ID('dbo.TradeIns'))
  CREATE INDEX IX_TradeIns_AutoGrade ON dbo.TradeIns (AutoGrade);

PRINT 'Trade-In schema fixes applied successfully!';

