-- Fix CreatedAt column type from datetime2 to datetimeoffset
USE AccessoryWorldDb;
GO

-- Check current column type
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TradeIns' AND COLUMN_NAME = 'CreatedAt';
GO

-- Alter the column type
ALTER TABLE [TradeIns] 
ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
GO

-- Verify the change
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TradeIns' AND COLUMN_NAME = 'CreatedAt';
GO

PRINT 'CreatedAt column successfully converted to datetimeoffset';
