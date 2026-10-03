using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeRecipeQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId",
                table: "RecipeSteps");

            migrationBuilder.DropIndex(
                name: "IX_RecipeImages_RecipeId",
                table: "RecipeImages");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "Recipes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CategoryId_IsDeleted_Status_CreatedAt",
                table: "Recipes",
                columns: new[] { "CategoryId", "IsDeleted", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedAt",
                table: "Recipes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_IsDeleted_Status_CreatedAt",
                table: "Recipes",
                columns: new[] { "IsDeleted", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_RecipeId_OrderIndex",
                table: "RecipeImages",
                columns: new[] { "RecipeId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CategoryId_IsDeleted_Status_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_IsDeleted_Status_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeImages_RecipeId_OrderIndex",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "Recipes");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId",
                table: "RecipeSteps",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_RecipeId",
                table: "RecipeImages",
                column: "RecipeId");
        }
    }
}
