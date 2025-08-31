using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SiteName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SiteEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    SmtpHost = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: true),
                    SmtpUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmtpPassword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SmtpEnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    RequireEmailConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    EnableTwoFactorAuth = table.Column<bool>(type: "bit", nullable: false),
                    SessionTimeoutMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    PaymentGateway = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentSecretKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EnablePayPal = table.Column<bool>(type: "bit", nullable: false),
                    EnableCreditCard = table.Column<bool>(type: "bit", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
