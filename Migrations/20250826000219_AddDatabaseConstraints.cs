using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_TradeInEvaluation_BaseValue_NonNegative",
                table: "TradeInEvaluations",
                sql: "[BaseValue] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradeInEvaluation_FinalOfferAmount_NonNegative",
                table: "TradeInEvaluations",
                sql: "[FinalOfferAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TradeInEvaluation_AccessoryBonus_NonNegative",
                table: "TradeInEvaluations",
                sql: "[AccessoryBonus] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SKU_LowStockThreshold_NonNegative",
                table: "SKUs",
                sql: "[LowStockThreshold] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SKU_Price_Positive",
                table: "SKUs",
                sql: "[Price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SKU_ReservedQuantity_Valid",
                table: "SKUs",
                sql: "[ReservedQuantity] >= 0 AND [ReservedQuantity] <= [StockQuantity]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SKU_StockQuantity_NonNegative",
                table: "SKUs",
                sql: "[StockQuantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Price_Positive",
                table: "Products",
                sql: "[Price] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_SalePrice_Valid",
                table: "Products",
                sql: "[SalePrice] IS NULL OR ([SalePrice] >= 0 AND [SalePrice] < [Price])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Amount_Positive",
                table: "Payments",
                sql: "[Amount] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_CreditNoteAmount_NonNegative",
                table: "Orders",
                sql: "[CreditNoteAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_DiscountAmount_Valid",
                table: "Orders",
                sql: "[DiscountAmount] >= 0 AND [DiscountAmount] <= [Subtotal]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_ShippingFee_NonNegative",
                table: "Orders",
                sql: "[ShippingFee] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Subtotal_Positive",
                table: "Orders",
                sql: "[Subtotal] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_Total_Positive",
                table: "Orders",
                sql: "[Total] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Order_TaxAmount_NonNegative",
                table: "Orders",
                sql: "[TaxAmount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_Quantity_Positive",
                table: "OrderItems",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_OrderItem_UnitPrice_NonNegative",
                table: "OrderItems",
                sql: "[UnitPrice] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DeviceModel_BaseTradeInValue_NonNegative",
                table: "DeviceModels",
                sql: "[BaseTradeInValue] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditNote_Amount_Positive",
                table: "CreditNotes",
                sql: "[Amount] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItem_Quantity_Positive",
                table: "CartItems",
                sql: "[Quantity] > 0 AND [Quantity] <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CartItem_UnitPrice_NonNegative",
                table: "CartItems",
                sql: "[UnitPrice] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TradeInEvaluation_BaseValue_NonNegative",
                table: "TradeInEvaluations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradeInEvaluation_FinalOfferAmount_NonNegative",
                table: "TradeInEvaluations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TradeInEvaluation_AccessoryBonus_NonNegative",
                table: "TradeInEvaluations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SKU_LowStockThreshold_NonNegative",
                table: "SKUs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SKU_Price_Positive",
                table: "SKUs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SKU_ReservedQuantity_Valid",
                table: "SKUs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SKU_StockQuantity_NonNegative",
                table: "SKUs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Price_Positive",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_SalePrice_Valid",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_CreditNoteAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_DiscountAmount_Valid",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_ShippingFee_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Subtotal_Positive",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_Total_Positive",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Order_TaxAmount_NonNegative",
                table: "Orders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_Quantity_Positive",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_OrderItem_UnitPrice_NonNegative",
                table: "OrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DeviceModel_BaseTradeInValue_NonNegative",
                table: "DeviceModels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditNote_Amount_Positive",
                table: "CreditNotes");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItem_Quantity_Positive",
                table: "CartItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CartItem_UnitPrice_NonNegative",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");
        }
    }
}
