using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CulinaryBlog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignCurrentRecipeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.RenameColumn(
                name: "Instructions",
                table: "Recipes",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "RecipeSteps",
                newName: "TimerMinutes");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Recipes",
                type: "tsvector",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION recipes_search_vector_update()
                RETURNS trigger AS $$
                BEGIN
                    NEW."SearchVector" :=
                        setweight(
                            to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))),
                            'A'
                        )
                        ||
                        setweight(
                            to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))),
                            'B'
                        );

                    RETURN NEW;
                END
                $$ LANGUAGE plpgsql;

                DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";

                CREATE TRIGGER trg_recipes_search_vector_update
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION recipes_search_vector_update();

                UPDATE "Recipes"
                SET "SearchVector" =
                    setweight(
                        to_tsvector('simple', unaccent(coalesce("Title", ''))),
                        'A'
                    )
                    ||
                    setweight(
                        to_tsvector('simple', unaccent(coalesce("Description", ''))),
                        'B'
                    );
                """);

            migrationBuilder.AlterColumn<string>(
                name: "OriginalUrl",
                table: "RecipeImages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "MediumUrl",
                table: "RecipeImages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "RecipeImages",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "OrderIndex",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsDeleted", "Name", "OrderIndex", "RowVersion", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Các món ăn chính trong bữa cơm Việt", null, false, "Món Chính", 1, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "mon-chinh", null },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Canh và súp truyền thống", null, false, "Món Canh", 2, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "mon-canh", null },
                    { new Guid("10000000-0000-0000-0000-000000000003"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Các loại bánh, chè và tráng miệng", null, false, "Món Tráng Miệng", 3, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "mon-trang-mieng", null },
                    { new Guid("10000000-0000-0000-0000-000000000004"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nước ép, sinh tố và thức uống", null, false, "Đồ Uống", 4, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "do-uong", null },
                    { new Guid("10000000-0000-0000-0000-000000000005"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Snack và đồ ăn nhẹ", null, false, "Ăn Vặt", 5, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, "an-vat", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecipeSteps_StepNumber",
                table: "RecipeSteps",
                sql: "\"StepNumber\" >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecipeSteps_TimerMinutes",
                table: "RecipeSteps",
                sql: "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_SearchVector",
                table: "Recipes",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeImages_RecipeId_IsPrimary",
                table: "RecipeImages",
                columns: new[] { "RecipeId", "IsPrimary" },
                unique: true,
                filter: "\"IsPrimary\" = true AND \"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecipeImages_OrderIndex",
                table: "RecipeImages",
                sql: "\"OrderIndex\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_OrderIndex",
                table: "Categories",
                column: "OrderIndex");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";
                DROP FUNCTION IF EXISTS recipes_search_vector_update();
                """);

            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeSteps_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeSteps_TimerMinutes",
                table: "RecipeSteps");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_SearchVector",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_RecipeImages_RecipeId_IsPrimary",
                table: "RecipeImages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeImages_OrderIndex",
                table: "RecipeImages");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_OrderIndex",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DropColumn(
                name: "TimerMinutes",
                table: "RecipeSteps");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "MediumUrl",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "Recipes",
                newName: "Instructions");

            migrationBuilder.RenameColumn(
                name: "TimerMinutes",
                table: "RecipeSteps",
                newName: "DurationMinutes");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "OriginalUrl",
                table: "RecipeImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048);

            migrationBuilder.AlterColumn<int>(
                name: "OrderIndex",
                table: "Categories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true);
        }
    }
}
