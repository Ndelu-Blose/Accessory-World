/* Add missing columns to CreditNotes table */
IF COL_LENGTH('dbo.CreditNotes','AmountRemaining') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD AmountRemaining decimal(18,2) NOT NULL DEFAULT(0);
  PRINT 'Added AmountRemaining column';
END;

IF COL_LENGTH('dbo.CreditNotes','NonWithdrawable') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD NonWithdrawable bit NOT NULL DEFAULT(1);
  PRINT 'Added NonWithdrawable column';
END;

IF COL_LENGTH('dbo.CreditNotes','StoreCreditOnly') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD StoreCreditOnly bit NOT NULL DEFAULT(1);
  PRINT 'Added StoreCreditOnly column';
END;

IF COL_LENGTH('dbo.CreditNotes','RedeemedAt') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD RedeemedAt datetimeoffset(7) NULL;
  PRINT 'Added RedeemedAt column';
END;

IF COL_LENGTH('dbo.CreditNotes','RedeemedOrderId') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD RedeemedOrderId int NULL;
  PRINT 'Added RedeemedOrderId column';
END;

IF COL_LENGTH('dbo.CreditNotes','ApplicationUserId') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD ApplicationUserId nvarchar(450) NULL;
  PRINT 'Added ApplicationUserId column';
END;

IF COL_LENGTH('dbo.CreditNotes','RowVersion') IS NULL
BEGIN
  ALTER TABLE dbo.CreditNotes ADD RowVersion rowversion;
  PRINT 'Added RowVersion column';
END;

PRINT 'All missing columns added successfully!';
