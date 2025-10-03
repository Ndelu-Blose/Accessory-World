/* --- CREDIT NOTES: add missing columns safely (idempotent) --- */
IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD AmountRemaining decimal(18,2) NOT NULL CONSTRAINT DF_CreditNotes_AmountRemaining DEFAULT(0);
END;

IF COL_LENGTH('dbo.CreditNotes','NonWithdrawable') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD NonWithdrawable bit NOT NULL CONSTRAINT DF_CreditNotes_NonWithdrawable DEFAULT(1);
END;

IF COL_LENGTH('dbo.CreditNotes','StoreCreditOnly') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD StoreCreditOnly bit NOT NULL CONSTRAINT DF_CreditNotes_StoreCreditOnly DEFAULT(1);
END;

IF COL_LENGTH('dbo.CreditNotes','RedeemedAt') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD RedeemedAt datetimeoffset(7) NULL;
END;

IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD RedeemedOrderId int NULL;
END;

IF COL_LENGTH('dbo.CreditNotes','TradeInId') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD TradeInId int NULL;
END;

IF COL_LENGTH('dbo.CreditNotes','RowVersion') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD RowVersion rowversion;
END;

/* Optional: initialize AmountRemaining from Amount if you have an Amount column */
IF COL_LENGTH('dbo.CreditNotes','Amount') IS NOT NULL
BEGIN
  UPDATE CN
    SET AmountRemaining = CN.Amount
  FROM dbo.CreditNotes CN
  WHERE CN.AmountRemaining = 0 AND CN.Amount > 0;
END;

/* --- Foreign keys / indexes (create only if targets exist and FK not already there) --- */
IF OBJECT_ID('dbo.TradeIns','U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CreditNotes_TradeIns_TradeInId')
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD CONSTRAINT FK_CreditNotes_TradeIns_TradeInId
    FOREIGN KEY (TradeInId) REFERENCES dbo.TradeIns(Id) ON DELETE SET NULL;
END;

IF OBJECT_ID('dbo.Orders','U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CreditNotes_Orders_RedeemedOrderId')
BEGIN
  ALTER TABLE dbo.CreditNotes
    ADD CONSTRAINT FK_CreditNotes_Orders_RedeemedOrderId
    FOREIGN KEY (RedeemedOrderId) REFERENCES dbo.Orders(Id) ON DELETE NO ACTION;
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CreditNotes_TradeInId' AND object_id = OBJECT_ID('dbo.CreditNotes'))
BEGIN
  CREATE INDEX IX_CreditNotes_TradeInId ON dbo.CreditNotes(TradeInId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CreditNotes_RedeemedOrderId' AND object_id = OBJECT_ID('dbo.CreditNotes'))
BEGIN
  CREATE INDEX IX_CreditNotes_RedeemedOrderId ON dbo.CreditNotes(RedeemedOrderId);
END;

PRINT 'CreditNotes schema fixes applied successfully!';
