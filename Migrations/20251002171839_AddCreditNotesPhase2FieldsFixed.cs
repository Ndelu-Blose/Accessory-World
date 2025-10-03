using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNotesPhase2FieldsFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "TradeIns",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "AutoGrade",
                table: "TradeIns",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AiVersion",
                table: "TradeIns",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AiVendor",
                table: "TradeIns",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AiRetryCount",
                table: "TradeIns",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "AiConfidence",
                table: "TradeIns",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdminApprovedAt",
                table: "TradeIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssessedAt",
                table: "TradeIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreditIssuedAt",
                table: "TradeIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreditNoteId",
                table: "TradeIns",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UserAcceptedAt",
                table: "TradeIns",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CreditNotes",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RedeemedAt",
                table: "CreditNotes",
                type: "datetimeoffset",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "CreditNotes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TradeInId1",
                table: "CreditNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TradeInId1",
                table: "CreditNotes",
                column: "TradeInId1");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_TradeIns_TradeInId1",
                table: "CreditNotes",
                column: "TradeInId1",
                principalTable: "TradeIns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditNotes_TradeIns_TradeInId1",
                table: "CreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_TradeInId1",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "AdminApprovedAt",
                table: "TradeIns");

            migrationBuilder.DropColumn(
                name: "AssessedAt",
                table: "TradeIns");

            migrationBuilder.DropColumn(
                name: "CreditIssuedAt",
                table: "TradeIns");

            migrationBuilder.DropColumn(
                name: "CreditNoteId",
                table: "TradeIns");

            migrationBuilder.DropColumn(
                name: "UserAcceptedAt",
                table: "TradeIns");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "TradeInId1",
                table: "CreditNotes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TradeIns",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "AutoGrade",
                table: "TradeIns",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AiVersion",
                table: "TradeIns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AiVendor",
                table: "TradeIns",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AiRetryCount",
                table: "TradeIns",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "AiConfidence",
                table: "TradeIns",
                type: "float",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "CreditNotes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RedeemedAt",
                table: "CreditNotes",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);
        }
    }
}
