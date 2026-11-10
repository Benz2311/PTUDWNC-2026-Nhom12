using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Keep existing device sessions together using their recorded rotation links.
            migrationBuilder.Sql("""
                WITH RECURSIVE family AS (
                    SELECT t."Id", t."UserId", t."ReplacedByTokenHash", t."Id" AS "RootId"
                    FROM "RefreshTokens" t
                    WHERE NOT EXISTS (
                        SELECT 1 FROM "RefreshTokens" parent
                        WHERE parent."UserId" = t."UserId"
                          AND parent."ReplacedByTokenHash" = t."TokenHash")
                    UNION
                    SELECT child."Id", child."UserId", child."ReplacedByTokenHash", family."RootId"
                    FROM family
                    JOIN "RefreshTokens" child ON child."UserId" = family."UserId"
                        AND child."TokenHash" = family."ReplacedByTokenHash"
                )
                UPDATE "RefreshTokens" token SET "FamilyId" = roots."RootId"
                FROM (
                    SELECT DISTINCT ON ("Id") "Id", "RootId" FROM family ORDER BY "Id", "RootId"
                ) roots
                WHERE token."Id" = roots."Id";

                UPDATE "RefreshTokens" SET "FamilyId" = "Id"
                WHERE "FamilyId" = '00000000-0000-0000-0000-000000000000';
                ALTER TABLE "RefreshTokens" ALTER COLUMN "FamilyId" DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyId",
                table: "RefreshTokens",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_FamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshTokens");
        }
    }
}
