using System;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104082500_AddPersonalPlanningV2")]
public partial class AddPersonalPlanningV2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MealPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MealPlans", x => x.Id);
                table.ForeignKey("FK_MealPlans_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "MealPlanItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MealPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanDate = table.Column<DateOnly>(type: "date", nullable: false),
                MealType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Servings = table.Column<int>(type: "integer", nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MealPlanItems", x => x.Id);
                table.ForeignKey("FK_MealPlanItems_MealPlans_MealPlanId", x => x.MealPlanId, "MealPlans", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_MealPlanItems_Recipes_RecipeId", x => x.RecipeId, "Recipes", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "ShoppingListId",
            table: "ShoppingListItems",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<Guid>(
            name: "RecipeId",
            table: "ShoppingListItems",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "Category",
            table: "ShoppingListItems",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);
        migrationBuilder.AddColumn<int>(
            name: "SortOrder",
            table: "ShoppingListItems",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "ShoppingLists",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShoppingLists", x => x.Id);
                table.ForeignKey("FK_ShoppingLists_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_MealPlans_UserId_StartDate_EndDate", "MealPlans", new[] { "UserId", "StartDate", "EndDate" });
        migrationBuilder.CreateIndex("IX_MealPlanItems_MealPlanId_PlanDate_MealType", "MealPlanItems", new[] { "MealPlanId", "PlanDate", "MealType" }, unique: true);
        migrationBuilder.CreateIndex("IX_MealPlanItems_RecipeId", "MealPlanItems", "RecipeId");
        migrationBuilder.CreateIndex("IX_ShoppingLists_UserId_CreatedAt", "ShoppingLists", new[] { "UserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ShoppingListItems_ShoppingListId_SortOrder", "ShoppingListItems", new[] { "ShoppingListId", "SortOrder" });
        migrationBuilder.CreateIndex("IX_ShoppingListItems_RecipeId", "ShoppingListItems", "RecipeId");

        migrationBuilder.AddForeignKey(
            name: "FK_ShoppingListItems_ShoppingLists_ShoppingListId",
            table: "ShoppingListItems",
            column: "ShoppingListId",
            principalTable: "ShoppingLists",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(
            name: "FK_ShoppingListItems_Recipes_RecipeId",
            table: "ShoppingListItems",
            column: "RecipeId",
            principalTable: "Recipes",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_ShoppingListItems_ShoppingLists_ShoppingListId", "ShoppingListItems");
        migrationBuilder.DropForeignKey("FK_ShoppingListItems_Recipes_RecipeId", "ShoppingListItems");
        migrationBuilder.DropTable("MealPlanItems");
        migrationBuilder.DropTable("MealPlans");
        migrationBuilder.DropTable("ShoppingLists");
        migrationBuilder.DropIndex("IX_ShoppingListItems_ShoppingListId_SortOrder", "ShoppingListItems");
        migrationBuilder.DropIndex("IX_ShoppingListItems_RecipeId", "ShoppingListItems");
        migrationBuilder.DropColumn("ShoppingListId", "ShoppingListItems");
        migrationBuilder.DropColumn("RecipeId", "ShoppingListItems");
        migrationBuilder.DropColumn("Category", "ShoppingListItems");
        migrationBuilder.DropColumn("SortOrder", "ShoppingListItems");
    }
}
