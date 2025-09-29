# Database Migration

> Generated on 2025-09-27. These instructions are Trae‑AI friendly: clear goals, inputs, steps, acceptance tests.


## Goal
Create normalized tables and constraints for TradeIns and CreditNotes, plus small changes to Stock tables.

## Inputs
- Existing SQL Server DB (AccessoryWorld).
- EF Core (>=7).

## Steps
1. Add Migrations:
   - Table **TradeIns**: 
     - Id UNIQUEIDENTIFIER PK (NEWSEQUENTIALID()), PublicId UNIQUEIDENTIFIER DEFAULT NEWID() UNIQUE,
     - CustomerId UNIQUEIDENTIFIER NOT NULL,
     - DeviceBrand NVARCHAR(64) NOT NULL DEFAULT 'Apple',
     - DeviceModel NVARCHAR(128) NOT NULL,
     - IMEI NVARCHAR(32) NULL,
     - ConditionGrade NVARCHAR(2) NOT NULL CHECK (ConditionGrade IN ('A','B','C','D')),
     - PhotosJson NVARCHAR(MAX) NOT NULL,
     - Status NVARCHAR(32) NOT NULL,
     - ProposedValue DECIMAL(18,2) NULL,
     - ApprovedValue DECIMAL(18,2) NULL,
     - Notes NVARCHAR(MAX) NULL,
     - CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
     - ReviewedAt DATETIME2 NULL,
     - ApprovedBy UNIQUEIDENTIFIER NULL,
     - RowVersion ROWVERSION.
   - Table **CreditNotes**:
     - Id UNIQUEIDENTIFIER PK (NEWSEQUENTIALID()),
     - Code NVARCHAR(32) NOT NULL UNIQUE,
     - CustomerId UNIQUEIDENTIFIER NOT NULL,
     - TradeInId UNIQUEIDENTIFIER NOT NULL UNIQUE, -- 1:1
     - Amount DECIMAL(18,2) NOT NULL,
     - AmountRemaining DECIMAL(18,2) NOT NULL,
     - Status NVARCHAR(32) NOT NULL,
     - ExpiresAt DATETIME2 NOT NULL,
     - CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
     - RedeemedAt DATETIME2 NULL,
     - RedeemedOrderId UNIQUEIDENTIFIER NULL,
     - RowVersion ROWVERSION.
   - **StockItems**: add IsTradeInUnit BIT NOT NULL DEFAULT 0, SourceTradeInId UNIQUEIDENTIFIER NULL (FK TradeIns.Id).
   - **StockMovements**: extend MovementType to include 'TradeInAdded','TradeInRefurbishedOut'.
2. Indexes:
   - IX_TradeIns_PublicId (UNIQUE), IX_TradeIns_CustomerId_Status, IX_CreditNotes_Code (UNIQUE),
     IX_CreditNotes_CustomerId_Status, IX_StockItems_SourceTradeInId.
3. FKs and Cascades:
   - CreditNotes.TradeInId → TradeIns.Id (ON DELETE CASCADE).

## Acceptance
- Migration adds tables/columns exactly as above.
- Unique constraint stops duplicate CreditNotes per TradeIn.
- Revert/redo migration works cleanly.
