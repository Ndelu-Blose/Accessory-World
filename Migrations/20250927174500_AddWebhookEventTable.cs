using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookEventTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RelatedOrderId = table.Column<int>(type: "int", nullable: true),
                    RelatedTradeInCaseId = table.Column<int>(type: "int", nullable: true),
                    RelatedCreditNoteId = table.Column<int>(type: "int", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_Orders_RelatedOrderId",
                        column: x => x.RelatedOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_TradeInCases_RelatedTradeInCaseId",
                        column: x => x.RelatedTradeInCaseId,
                        principalTable: "TradeInCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WebhookEvents_CreditNotes_RelatedCreditNoteId",
                        column: x => x.RelatedCreditNoteId,
                        principalTable: "CreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventId",
                table: "WebhookEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_EventType_Source",
                table: "WebhookEvents",
                columns: new[] { "EventType", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_Status",
                table: "WebhookEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_ReceivedAt",
                table: "WebhookEvents",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_RelatedOrderId",
                table: "WebhookEvents",
                column: "RelatedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_RelatedTradeInCaseId",
                table: "WebhookEvents",
                column: "RelatedTradeInCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_RelatedCreditNoteId",
                table: "WebhookEvents",
                column: "RelatedCreditNoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookEvents");
        }
    }
}