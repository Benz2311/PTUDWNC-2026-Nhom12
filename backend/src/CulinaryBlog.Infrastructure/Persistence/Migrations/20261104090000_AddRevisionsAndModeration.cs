using System;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104090000_AddRevisionsAndModeration")]
public partial class AddRevisionsAndModeration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "RecipeComments",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Approved");

        migrationBuilder.CreateTable(
            name: "ModerationLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TargetType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                ModeratorId = table.Column<Guid>(type: "uuid", nullable: false),
                Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModerationLogs", x => x.Id);
                table.ForeignKey("FK_ModerationLogs_AspNetUsers_ModeratorId", x => x.ModeratorId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "RecipeRevisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                EditorId = table.Column<Guid>(type: "uuid", nullable: false),
                ChangeSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeRevisions", x => x.Id);
                table.ForeignKey("FK_RecipeRevisions_AspNetUsers_EditorId", x => x.EditorId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_RecipeRevisions_Recipes_RecipeId", x => x.RecipeId, "Recipes", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ModerationLogs_TargetType_TargetId_CreatedAt", "ModerationLogs", new[] { "TargetType", "TargetId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ModerationLogs_ModeratorId", "ModerationLogs", "ModeratorId");
        migrationBuilder.CreateIndex("IX_RecipeRevisions_RecipeId_Version", "RecipeRevisions", new[] { "RecipeId", "Version" }, unique: true);
        migrationBuilder.CreateIndex("IX_RecipeRevisions_EditorId", "RecipeRevisions", "EditorId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ModerationLogs");
        migrationBuilder.DropTable("RecipeRevisions");
        migrationBuilder.DropColumn("Status", "RecipeComments");
    }
}
