using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261003090000_AddRecipeLifecycleSupport")]
public sealed class AddRecipeLifecycleSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DeletedAt",
            table: "Recipes",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "RecipeSlugHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeSlugHistories", item => item.Id);
                table.ForeignKey(
                    name: "FK_RecipeSlugHistories_Recipes_RecipeId",
                    column: item => item.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RecipeAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RecipeAuditLogs", item => item.Id));

        migrationBuilder.CreateIndex(
            name: "IX_RecipeSlugHistories_RecipeId_Slug",
            table: "RecipeSlugHistories",
            columns: new[] { "RecipeId", "Slug" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_RecipeSlugHistories_Slug",
            table: "RecipeSlugHistories",
            column: "Slug");
        migrationBuilder.CreateIndex(
            name: "IX_RecipeAuditLogs_RecipeId_CreatedAt",
            table: "RecipeAuditLogs",
            columns: new[] { "RecipeId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RecipeAuditLogs");
        migrationBuilder.DropTable(name: "RecipeSlugHistories");
        migrationBuilder.DropColumn(name: "DeletedAt", table: "Recipes");
    }
}
