using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeInAndCreditNoteTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeIns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CustomerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeviceBrand = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "Apple"),
                    DeviceModel = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IMEI = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ConditionGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PhotosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ApprovedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeIns", x => x.Id);
                    table.CheckConstraint("CK_TradeIn_ConditionGrade", "[ConditionGrade] IN ('A','B','C','D')");
                    table.ForeignKey(
                        name: "FK_TradeIns_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TradeIns_AspNetUsers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeIns_ApprovedBy",
                table: "TradeIns",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TradeIns_CustomerId_Status",
                table: "TradeIns",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TradeIns_PublicId",
                table: "TradeIns",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeIns");
        }
    }
}
