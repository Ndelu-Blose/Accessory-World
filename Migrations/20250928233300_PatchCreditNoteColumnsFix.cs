using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class PatchCreditNoteColumnsFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns to CreditNotes table if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'RedeemedAt')
                BEGIN
                    ALTER TABLE [CreditNotes] ADD [RedeemedAt] datetime2 NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'RedeemedOrderId')
                BEGIN
                    ALTER TABLE [CreditNotes] ADD [RedeemedOrderId] uniqueidentifier NULL;
                END
            ");

            // Drop problematic shadow foreign key columns if they exist
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'TradeInCaseId1')
                BEGIN
                    ALTER TABLE [CreditNotes] DROP COLUMN [TradeInCaseId1];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'ApplicationUserId')
                BEGIN
                    ALTER TABLE [CreditNotes] DROP COLUMN [ApplicationUserId];
                END
            ");

            // Ensure proper indexes exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'IX_CreditNotes_TradeInId')
                BEGIN
                    CREATE UNIQUE INDEX [IX_CreditNotes_TradeInId] ON [CreditNotes] ([TradeInId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the columns we added
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'RedeemedAt')
                BEGIN
                    ALTER TABLE [CreditNotes] DROP COLUMN [RedeemedAt];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'RedeemedOrderId')
                BEGIN
                    ALTER TABLE [CreditNotes] DROP COLUMN [RedeemedOrderId];
                END
            ");

            // Drop the unique index
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[CreditNotes]') AND name = 'IX_CreditNotes_TradeInId')
                BEGIN
                    DROP INDEX [IX_CreditNotes_TradeInId] ON [CreditNotes];
                END
            ");
        }
    }
}