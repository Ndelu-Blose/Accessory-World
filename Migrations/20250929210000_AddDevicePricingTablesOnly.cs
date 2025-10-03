using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddDevicePricingTablesOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceModelCatalogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReleaseYear = table.Column<int>(type: "int", nullable: false),
                    StorageGb = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceModelCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceAdjustmentRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    FlatDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AppliesTo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, defaultValue: "ANY"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceAdjustmentRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceBasePrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceModelCatalogId = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AsOf = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceBasePrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceBasePrices_DeviceModelCatalogs_DeviceModelCatalogId",
                        column: x => x.DeviceModelCatalogId,
                        principalTable: "DeviceModelCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceBasePrices_DeviceModelCatalogId",
                table: "DeviceBasePrices",
                column: "DeviceModelCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceModelCatalogs_Brand_Model_DeviceType",
                table: "DeviceModelCatalogs",
                columns: new[] { "Brand", "Model", "DeviceType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceAdjustmentRules_Code",
                table: "PriceAdjustmentRules",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_PriceAdjustmentRules_IsActive",
                table: "PriceAdjustmentRules",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceBasePrices");

            migrationBuilder.DropTable(
                name: "DeviceModelCatalogs");

            migrationBuilder.DropTable(
                name: "PriceAdjustmentRules");
        }
    }
}