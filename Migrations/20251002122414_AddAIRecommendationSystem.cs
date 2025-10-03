using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessoryWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddAIRecommendationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSimilarities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId1 = table.Column<int>(type: "int", nullable: false),
                    ProductId2 = table.Column<int>(type: "int", nullable: false),
                    SimilarityScore = table.Column<double>(type: "float", nullable: false),
                    SimilarityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSimilarities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSimilarities_Products_ProductId1",
                        column: x => x.ProductId1,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductSimilarities_Products_ProductId2",
                        column: x => x.ProductId2,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecommendationModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    AlgorithmType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TestGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TestVariant = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationModels_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendationModels_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserBehaviors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SearchQuery = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    SessionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBehaviors_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBehaviors_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreferredCategories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreferredBrands = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PriceRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShoppingStyle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AverageOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PurchaseFrequency = table.Column<int>(type: "int", nullable: true),
                    PreferredDeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecommendationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FeedbackType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationFeedbacks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecommendationFeedbacks_RecommendationModels_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "RecommendationModels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilarities_CalculatedAt",
                table: "ProductSimilarities",
                column: "CalculatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilarities_ProductId1_ProductId2_SimilarityType",
                table: "ProductSimilarities",
                columns: new[] { "ProductId1", "ProductId2", "SimilarityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilarities_ProductId1_SimilarityType",
                table: "ProductSimilarities",
                columns: new[] { "ProductId1", "SimilarityType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilarities_ProductId2_SimilarityType",
                table: "ProductSimilarities",
                columns: new[] { "ProductId2", "SimilarityType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSimilarities_SimilarityType",
                table: "ProductSimilarities",
                column: "SimilarityType");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_RecommendationId_FeedbackType",
                table: "RecommendationFeedbacks",
                columns: new[] { "RecommendationId", "FeedbackType" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_Timestamp",
                table: "RecommendationFeedbacks",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedbacks_UserId",
                table: "RecommendationFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationModels_AlgorithmType_GeneratedAt",
                table: "RecommendationModels",
                columns: new[] { "AlgorithmType", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationModels_IsActive",
                table: "RecommendationModels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationModels_ProductId",
                table: "RecommendationModels",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationModels_TestGroup",
                table: "RecommendationModels",
                column: "TestGroup");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationModels_UserId_GeneratedAt",
                table: "RecommendationModels",
                columns: new[] { "UserId", "GeneratedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviors_ActionType",
                table: "UserBehaviors",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviors_ProductId_ActionType",
                table: "UserBehaviors",
                columns: new[] { "ProductId", "ActionType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviors_SessionId",
                table: "UserBehaviors",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviors_UserId_Timestamp",
                table: "UserBehaviors",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_LastUpdated",
                table: "UserProfiles",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSimilarities");

            migrationBuilder.DropTable(
                name: "RecommendationFeedbacks");

            migrationBuilder.DropTable(
                name: "UserBehaviors");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "RecommendationModels");
        }
    }
}
