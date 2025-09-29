using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureAddressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefault" },
                unique: true,
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefault",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefault" });
        }
    }
}
