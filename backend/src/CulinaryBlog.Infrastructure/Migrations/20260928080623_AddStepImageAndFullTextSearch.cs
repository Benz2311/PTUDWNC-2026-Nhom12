using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStepImageAndFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            // =========================================================
            // 1. RECIPESTEPS (TV4)
            // =========================================================

            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "RecipeSteps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "RecipeSteps",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimerMinutes",
                table: "RecipeSteps",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecipeSteps_TimerMinutes",
                table: "RecipeSteps",
                sql: "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");

            // =========================================================
            // 2. RECIPEIMAGES (TV4)
            // =========================================================

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeImages_SortOrder",
                table: "RecipeImages");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "RecipeImages",
                newName: "OriginalUrl");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "RecipeImages",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeImages_RecipeId_SortOrder",
                table: "RecipeImages",
                newName: "IX_RecipeImages_RecipeId_OrderIndex");

            migrationBuilder.AlterColumn<string>(
                name: "AltText",
                table: "RecipeImages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

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

            // =========================================================
            // 3. RECIPES FULL-TEXT SEARCH (TV4)
            // =========================================================

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Recipes",
                type: "tsvector",
                nullable: true);

            // Trigger Function
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION recipes_search_vector_update()
                RETURNS trigger AS $$
                BEGIN
                    NEW."SearchVector" :=
                        setweight(
                            to_tsvector(
                                'simple',
                                unaccent(coalesce(NEW."Title", ''))
                            ),
                            'A'
                        )
                        ||
                        setweight(
                            to_tsvector(
                                'simple',
                                unaccent(coalesce(NEW."Description", ''))
                            ),
                            'B'
                        );

                    RETURN NEW;
                END
                $$ LANGUAGE plpgsql;
                """);

            // Trigger
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";

                CREATE TRIGGER trg_recipes_search_vector_update
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION recipes_search_vector_update();
                """);

            // Backfill SearchVector cho Recipe hiện có
            migrationBuilder.Sql("""
                UPDATE "Recipes"
                SET "SearchVector" =
                    setweight(
                        to_tsvector(
                            'simple',
                            unaccent(coalesce("Title", ''))
                        ),
                        'A'
                    )
                    ||
                    setweight(
                        to_tsvector(
                            'simple',
                            unaccent(coalesce("Description", ''))
                        ),
                        'B'
                    );
                """);

            // GIN Index
            migrationBuilder.CreateIndex(
                name: "IX_Recipes_SearchVector",
                table: "Recipes",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop Trigger và Function FTS trước
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";
                DROP FUNCTION IF EXISTS recipes_search_vector_update();
                """);

            // Rollback Recipes FTS
            migrationBuilder.DropIndex(
                name: "IX_Recipes_SearchVector",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                table: "Recipes");

            // Rollback RecipeSteps
            migrationBuilder.DropIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeSteps_TimerMinutes",
                table: "RecipeSteps");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "RecipeSteps");

            migrationBuilder.DropColumn(
                name: "TimerMinutes",
                table: "RecipeSteps");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "RecipeSteps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeSteps_RecipeId_StepNumber",
                table: "RecipeSteps",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true);

            // Rollback RecipeImages
            migrationBuilder.DropIndex(
                name: "IX_RecipeImages_RecipeId_IsPrimary",
                table: "RecipeImages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RecipeImages_OrderIndex",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "MediumUrl",
                table: "RecipeImages");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "RecipeImages");

            migrationBuilder.RenameColumn(
                name: "OriginalUrl",
                table: "RecipeImages",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "RecipeImages",
                newName: "SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_RecipeImages_RecipeId_OrderIndex",
                table: "RecipeImages",
                newName: "IX_RecipeImages_RecipeId_SortOrder");

            migrationBuilder.AlterColumn<string>(
                name: "AltText",
                table: "RecipeImages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RecipeImages_SortOrder",
                table: "RecipeImages",
                sql: "\"SortOrder\" >= 0");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
