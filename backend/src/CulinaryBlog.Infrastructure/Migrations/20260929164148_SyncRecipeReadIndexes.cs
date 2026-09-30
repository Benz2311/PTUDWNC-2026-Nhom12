using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncRecipeReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON \"Recipes\";");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Recipes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Recipes",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_recipes_search_vector_update
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION recipes_search_vector_update();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CategoryId_IsDeleted_Status_CreatedAt",
                table: "Recipes",
                columns: new[] { "CategoryId", "IsDeleted", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_CreatedAt",
                table: "Recipes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Description",
                table: "Recipes",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_IsDeleted_Status_CreatedAt",
                table: "Recipes",
                columns: new[] { "IsDeleted", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Slug",
                table: "Recipes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Status",
                table: "Recipes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Title",
                table: "Recipes",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_CategoryId_IsDeleted_Status_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Description",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_IsDeleted_Status_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Slug",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Status",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Recipes_Title",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON \"Recipes\";");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Recipes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Recipes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220);

            migrationBuilder.Sql("""
                CREATE TRIGGER trg_recipes_search_vector_update
                BEFORE INSERT OR UPDATE OF "Title", "Description"
                ON "Recipes"
                FOR EACH ROW
                EXECUTE FUNCTION recipes_search_vector_update();
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name");
        }
    }
}
