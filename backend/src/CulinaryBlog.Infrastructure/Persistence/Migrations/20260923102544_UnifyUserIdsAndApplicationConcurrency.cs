using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260923102544_UnifyUserIdsAndApplicationConcurrency")]
public class UnifyUserIdsAndApplicationConcurrency : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"AspNetUsers\" WHERE \"Id\" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') THEN RAISE EXCEPTION 'Cannot convert AspNetUsers.Id to uuid: invalid value found'; END IF; IF EXISTS (SELECT 1 FROM \"RefreshTokens\" WHERE \"UserId\" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') THEN RAISE EXCEPTION 'Cannot convert RefreshTokens.UserId to uuid: invalid value found'; END IF; IF EXISTS (SELECT 1 FROM \"Recipes\" WHERE \"AuthorId\" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') THEN RAISE EXCEPTION 'Cannot convert Recipes.AuthorId to uuid: invalid value found'; END IF; END $$;", false);
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" DROP CONSTRAINT \"FK_RefreshTokens_AspNetUsers_UserId\";", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" DROP CONSTRAINT \"FK_Recipes_AspNetUsers_AuthorId\";", false);
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" ALTER COLUMN \"UserId\" TYPE uuid USING \"UserId\"::uuid;", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" ALTER COLUMN \"AuthorId\" TYPE uuid USING \"AuthorId\"::uuid;", false);
		migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"Id\" TYPE uuid USING \"Id\"::uuid;", false);
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" ADD CONSTRAINT \"FK_RefreshTokens_AspNetUsers_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"AspNetUsers\" (\"Id\") ON DELETE CASCADE;", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" ADD CONSTRAINT \"FK_Recipes_AspNetUsers_AuthorId\" FOREIGN KEY (\"AuthorId\") REFERENCES \"AspNetUsers\" (\"Id\") ON DELETE RESTRICT;", false);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" DROP CONSTRAINT \"FK_RefreshTokens_AspNetUsers_UserId\";", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" DROP CONSTRAINT \"FK_Recipes_AspNetUsers_AuthorId\";", false);
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" ALTER COLUMN \"UserId\" TYPE text USING \"UserId\"::text;", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" ALTER COLUMN \"AuthorId\" TYPE text USING \"AuthorId\"::text;", false);
		migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ALTER COLUMN \"Id\" TYPE text USING \"Id\"::text;", false);
		migrationBuilder.Sql("ALTER TABLE \"RefreshTokens\" ADD CONSTRAINT \"FK_RefreshTokens_AspNetUsers_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"AspNetUsers\" (\"Id\") ON DELETE CASCADE;", false);
		migrationBuilder.Sql("ALTER TABLE \"Recipes\" ADD CONSTRAINT \"FK_Recipes_AspNetUsers_AuthorId\" FOREIGN KEY (\"AuthorId\") REFERENCES \"AspNetUsers\" (\"Id\") ON DELETE RESTRICT;", false);
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", (object)"10.0.0").HasAnnotation("Relational:MaxIdentifierLength", (object)63);
		NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.ApplicationUser", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("AccessFailedCount"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("AvatarUrl").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ConcurrencyStamp"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Email").IsRequired(true).HasMaxLength(256), "character varying(256)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("EmailConfirmed"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("FullName").IsRequired(true).HasMaxLength(100), "character varying(100)"), "DisplayName");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsActive"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("LockoutEnabled"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTimeOffset?>(b.Property<DateTimeOffset?>("LockoutEnd"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("NormalizedEmail"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("NormalizedUserName"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("PasswordHash"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("PhoneNumber"), "text");
			RelationalPrimitiveCollectionBuilderExtensions.HasColumnType<string[]>(b.PrimitiveCollection<string[]>("Roles").IsRequired(true), "text[]");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("SecurityStamp"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("TwoFactorEnabled"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("UserName").IsRequired(true).HasMaxLength(256), "character varying(256)");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "Email" }).IsUnique(true);
			b.HasIndex(new string[1] { "UserName" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "AspNetUsers", (string)null);
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Category", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description"), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Name").IsRequired(true).HasMaxLength(100), "character varying(100)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("OrderIndex"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnName<byte[]>(RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true), "bytea"), "RowVersion");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Slug").IsRequired(true).HasMaxLength(120), "character varying(120)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "Slug" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Categories");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Recipe", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("AuthorId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("CategoryId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Content").IsRequired(true), "text"), "Instructions");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("CookTimeMinutes"), "integer"), "CookTimeMinutes");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description").IsRequired(true), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Difficulty"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("PrepTimeMinutes"), "integer"), "PrepTimeMinutes");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("PublishedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnName<byte[]>(RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true), "bytea"), "RowVersion");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Servings"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Slug").IsRequired(true).HasMaxLength(220), "character varying(220)");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Status"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Title").IsRequired(true).HasMaxLength(200), "character varying(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "AuthorId" });
			b.HasIndex(new string[1] { "CategoryId" });
			b.HasIndex(new string[1] { "Slug" }).IsUnique(true);
			b.HasIndex(new string[1] { "Status" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Recipes");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeImage", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("AltText").HasMaxLength(200), "character varying(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsPrimary"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("MediumUrl").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("RecipeId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true), "bytea");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("SortOrder"), "integer"), "OrderIndex");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ThumbnailUrl").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Url").IsRequired(true).HasMaxLength(500), "character varying(500)"), "OriginalUrl");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "RecipeId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "RecipeImages");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeIngredient", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Name").IsRequired(true).HasMaxLength(200), "character varying(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Notes").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(b.Property<decimal?>("Quantity").HasPrecision(10, 3), "numeric(10,3)");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("RecipeId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true), "bytea");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("SortOrder"), "integer"), "OrderIndex");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Unit").HasMaxLength(50), "character varying(50)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "RecipeId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "RecipeIngredients");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeStep", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description").IsRequired(true), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ImageUrl").HasMaxLength(500), "character varying(500)");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("RecipeId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true), "bytea");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("StepNumber"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnName<int?>(RelationalPropertyBuilderExtensions.HasColumnType<int?>(b.Property<int?>("TimerMinutes"), "integer"), "DurationMinutes");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Title").IsRequired(true).HasMaxLength(200), "character varying(200)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "RecipeId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "RecipeSteps");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RefreshToken", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("CreatedByIp").HasMaxLength(45), "character varying(45)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("ExpiresAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsRevoked"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("ReplacedByToken").HasMaxLength(64), "character varying(64)"), "ReplacedByTokenHash");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("RevokedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Token").IsRequired(true).HasMaxLength(64), "character varying(64)"), "TokenHash");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("UserId"), "uuid");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "Token" }).IsUnique(true);
			b.HasIndex(new string[1] { "UserId" });
			RelationalEntityTypeBuilderExtensions.ToTable(b, "RefreshTokens");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Recipe", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.HasOne("CulinaryBlog.Domain.Entities.ApplicationUser", "Author").WithMany("Recipes").HasForeignKey(new string[1] { "AuthorId" })
				.OnDelete((DeleteBehavior)1)
				.IsRequired(true);
			b.HasOne("CulinaryBlog.Domain.Entities.Category", "Category").WithMany("Recipes").HasForeignKey(new string[1] { "CategoryId" })
				.OnDelete((DeleteBehavior)1)
				.IsRequired(true);
			b.OwnsOne("CulinaryBlog.Domain.Entities.RecipeNutrition", "Nutrition", (Action<OwnedNavigationBuilder>)((OwnedNavigationBuilder val) =>
			{
				RelationalPropertyBuilderExtensions.HasColumnType<Guid>(val.Property<Guid>("RecipeId"), "uuid");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Calories").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Calories");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Carbohydrates").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Carbohydrates");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Fat").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Fat");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Fiber").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Fiber");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Protein").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Protein");
				RelationalPropertyBuilderExtensions.HasColumnName<decimal?>(RelationalPropertyBuilderExtensions.HasColumnType<decimal?>(val.Property<decimal?>("Sodium").HasPrecision(8, 2), "numeric(8,2)"), "Nutrition_Sodium");
				val.HasKey(new string[1] { "RecipeId" });
				RelationalEntityTypeBuilderExtensions.ToTable(val, "Recipes");
				val.WithOwner((string)null).HasForeignKey(new string[1] { "RecipeId" });
			}));
			b.Navigation("Author");
			b.Navigation("Category");
			b.Navigation("Nutrition").IsRequired(true);
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeImage", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.HasOne("CulinaryBlog.Domain.Entities.Recipe", "Recipe").WithMany("Images").HasForeignKey(new string[1] { "RecipeId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Recipe");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeIngredient", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.HasOne("CulinaryBlog.Domain.Entities.Recipe", "Recipe").WithMany("Ingredients").HasForeignKey(new string[1] { "RecipeId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Recipe");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RecipeStep", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.HasOne("CulinaryBlog.Domain.Entities.Recipe", "Recipe").WithMany("Steps").HasForeignKey(new string[1] { "RecipeId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("Recipe");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.RefreshToken", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.HasOne("CulinaryBlog.Domain.Entities.ApplicationUser", "User").WithMany("RefreshTokens").HasForeignKey(new string[1] { "UserId" })
				.OnDelete((DeleteBehavior)3)
				.IsRequired(true);
			b.Navigation("User");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.ApplicationUser", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.Navigation("Recipes");
			b.Navigation("RefreshTokens");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Category", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.Navigation("Recipes");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Recipe", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			b.Navigation("Images");
			b.Navigation("Ingredients");
			b.Navigation("Steps");
		}));
	}
}
