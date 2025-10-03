-- Fix CreatedAt column type from datetime2 to datetimeoffset
USE AccessoryWorldDb;
GO

-- Check current column type
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TradeIns' AND COLUMN_NAME = 'CreatedAt';
GO

-- Find and drop the default constraint
DECLARE @constraintName NVARCHAR(128);
SELECT @constraintName = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TradeIns]') AND [c].[name] = N'CreatedAt');

IF @constraintName IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE [TradeIns] DROP CONSTRAINT [' + @constraintName + ']');
    PRINT 'Dropped constraint: ' + @constraintName;
END
ELSE
BEGIN
    PRINT 'No default constraint found for CreatedAt column';
END
GO

-- Alter the column type
ALTER TABLE [TradeIns] 
ALTER COLUMN [CreatedAt] datetimeoffset NOT NULL;
GO

-- Add new default constraint
ALTER TABLE [TradeIns] 
ADD DEFAULT (SYSUTCDATETIME()) FOR [CreatedAt];
GO

-- Verify the change
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TradeIns' AND COLUMN_NAME = 'CreatedAt';
GO

PRINT 'CreatedAt column successfully converted to datetimeoffset with new default constraint';
