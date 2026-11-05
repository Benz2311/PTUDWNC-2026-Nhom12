using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104100000_ExpandRecipeSearchVector")]
public partial class ExpandRecipeSearchVector : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION recipes_search_vector_update()
            RETURNS trigger AS $$
            BEGIN
                NEW."SearchVector" :=
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))), 'A')
                    || setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))), 'B')
                    || setweight(to_tsvector('simple', unaccent(coalesce(NEW."Content", ''))), 'C')
                    || setweight(to_tsvector('simple', unaccent(coalesce((
                        SELECT string_agg(i."Name", ' ')
                        FROM "RecipeIngredients" i
                        WHERE i."RecipeId" = NEW."Id" AND NOT i."IsDeleted"
                    ), ''))), 'B')
                    || setweight(to_tsvector('simple', unaccent(coalesce((
                        SELECT string_agg(concat_ws(' ', s."Title", s."Description"), ' ')
                        FROM "RecipeSteps" s
                        WHERE s."RecipeId" = NEW."Id" AND NOT s."IsDeleted"
                    ), ''))), 'C');
                RETURN NEW;
            END
            $$ LANGUAGE plpgsql;

            CREATE OR REPLACE FUNCTION recipes_search_children_update()
            RETURNS trigger AS $$
            DECLARE
                target_recipe_id uuid;
            BEGIN
                target_recipe_id := CASE WHEN TG_OP = 'DELETE' THEN OLD."RecipeId" ELSE NEW."RecipeId" END;
                UPDATE "Recipes" r
                SET "SearchVector" =
                    setweight(to_tsvector('simple', unaccent(coalesce(r."Title", ''))), 'A')
                    || setweight(to_tsvector('simple', unaccent(coalesce(r."Description", ''))), 'B')
                    || setweight(to_tsvector('simple', unaccent(coalesce(r."Content", ''))), 'C')
                    || setweight(to_tsvector('simple', unaccent(coalesce((
                        SELECT string_agg(i."Name", ' ')
                        FROM "RecipeIngredients" i
                        WHERE i."RecipeId" = r."Id" AND NOT i."IsDeleted"
                    ), ''))), 'B')
                    || setweight(to_tsvector('simple', unaccent(coalesce((
                        SELECT string_agg(concat_ws(' ', s."Title", s."Description"), ' ')
                        FROM "RecipeSteps" s
                        WHERE s."RecipeId" = r."Id" AND NOT s."IsDeleted"
                    ), ''))), 'C')
                WHERE r."Id" = target_recipe_id;
                RETURN NULL;
            END
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";
            CREATE TRIGGER trg_recipes_search_vector_update
            BEFORE INSERT OR UPDATE OF "Title", "Description", "Content"
            ON "Recipes"
            FOR EACH ROW EXECUTE FUNCTION recipes_search_vector_update();

            DROP TRIGGER IF EXISTS trg_recipe_ingredients_search_update ON "RecipeIngredients";
            CREATE TRIGGER trg_recipe_ingredients_search_update
            AFTER INSERT OR UPDATE OR DELETE ON "RecipeIngredients"
            FOR EACH ROW EXECUTE FUNCTION recipes_search_children_update();

            DROP TRIGGER IF EXISTS trg_recipe_steps_search_update ON "RecipeSteps";
            CREATE TRIGGER trg_recipe_steps_search_update
            AFTER INSERT OR UPDATE OR DELETE ON "RecipeSteps"
            FOR EACH ROW EXECUTE FUNCTION recipes_search_children_update();

            UPDATE "Recipes" r
            SET "SearchVector" =
                setweight(to_tsvector('simple', unaccent(coalesce(r."Title", ''))), 'A')
                || setweight(to_tsvector('simple', unaccent(coalesce(r."Description", ''))), 'B')
                || setweight(to_tsvector('simple', unaccent(coalesce(r."Content", ''))), 'C')
                || setweight(to_tsvector('simple', unaccent(coalesce((
                    SELECT string_agg(i."Name", ' ')
                    FROM "RecipeIngredients" i
                    WHERE i."RecipeId" = r."Id" AND NOT i."IsDeleted"
                ), ''))), 'B')
                || setweight(to_tsvector('simple', unaccent(coalesce((
                    SELECT string_agg(concat_ws(' ', s."Title", s."Description"), ' ')
                    FROM "RecipeSteps" s
                    WHERE s."RecipeId" = r."Id" AND NOT s."IsDeleted"
                ), ''))), 'C');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_recipe_steps_search_update ON "RecipeSteps";
            DROP TRIGGER IF EXISTS trg_recipe_ingredients_search_update ON "RecipeIngredients";
            DROP FUNCTION IF EXISTS recipes_search_children_update();

            CREATE OR REPLACE FUNCTION recipes_search_vector_update()
            RETURNS trigger AS $$
            BEGIN
                NEW."SearchVector" :=
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))), 'A')
                    || setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))), 'B');
                RETURN NEW;
            END
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS trg_recipes_search_vector_update ON "Recipes";
            CREATE TRIGGER trg_recipes_search_vector_update
            BEFORE INSERT OR UPDATE OF "Title", "Description"
            ON "Recipes"
            FOR EACH ROW EXECUTE FUNCTION recipes_search_vector_update();

            UPDATE "Recipes"
            SET "SearchVector" =
                setweight(to_tsvector('simple', unaccent(coalesce("Title", ''))), 'A')
                || setweight(to_tsvector('simple', unaccent(coalesce("Description", ''))), 'B');
            """);
    }
}
