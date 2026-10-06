using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261003100000_AddRecipeNutritionSource")]
public sealed class AddRecipeNutritionSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Nutrition_Source",
            table: "Recipes",
            type: "character varying(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Recipes\" SET \"Nutrition_Source\" = 'Manual' WHERE \"Nutrition_Source\" IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "Nutrition_Source",
            table: "Recipes",
            type: "character varying(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(40)",
            oldMaxLength: 40,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Nutrition_Source",
            table: "Recipes");
    }
}
