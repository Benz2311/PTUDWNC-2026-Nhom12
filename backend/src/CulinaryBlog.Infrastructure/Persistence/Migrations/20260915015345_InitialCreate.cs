using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260915015345_InitialCreate")]
public class InitialCreate : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.CreateTable("AspNetUsers", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> normalizedUserName = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> normalizedEmail = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> securityStamp = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> concurrencyStamp = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> phoneNumber = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> twoFactorEnabled = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> lockoutEnd = table.Column<DateTimeOffset>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> lockoutEnabled = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> accessFailedCount = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 100;
			OperationBuilder<AddColumnOperation> displayName = table.Column<string>("character varying(100)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 256;
			OperationBuilder<AddColumnOperation> userName = table.Column<string>("character varying(256)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 256;
			OperationBuilder<AddColumnOperation> email = table.Column<string>("character varying(256)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 500;
			return new
			{
				Id = id,
				NormalizedUserName = normalizedUserName,
				NormalizedEmail = normalizedEmail,
				SecurityStamp = securityStamp,
				ConcurrencyStamp = concurrencyStamp,
				PhoneNumber = phoneNumber,
				TwoFactorEnabled = twoFactorEnabled,
				LockoutEnd = lockoutEnd,
				LockoutEnabled = lockoutEnabled,
				AccessFailedCount = accessFailedCount,
				DisplayName = displayName,
				UserName = userName,
				Email = email,
				AvatarUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				PasswordHash = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				EmailConfirmed = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				Roles = table.Column<string[]>("text[]", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsActive = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_AspNetUsers", x => (object)x.Id);
		}, (string)null);
		migrationBuilder.CreateTable("Categories", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 100;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("character varying(100)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 120;
			OperationBuilder<AddColumnOperation> slug = table.Column<string>("character varying(120)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> description = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 500;
			return new
			{
				Id = id,
				Name = name,
				Slug = slug,
				Description = description,
				ImageUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				OrderIndex = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				UpdatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsDeleted = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				RowVersion = table.Column<byte[]>("bytea", (bool?)null, (int?)null, true, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_Categories", x => (object)x.Id);
		}, (string)null);
		migrationBuilder.CreateTable("RefreshTokens", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> userId = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 64;
			OperationBuilder<AddColumnOperation> tokenHash = table.Column<string>("character varying(64)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> expiresAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> revokedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> isRevoked = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 64;
			OperationBuilder<AddColumnOperation> replacedByTokenHash = table.Column<string>("character varying(64)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> createdAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 45;
			return new
			{
				Id = id,
				UserId = userId,
				TokenHash = tokenHash,
				ExpiresAt = expiresAt,
				RevokedAt = revokedAt,
				IsRevoked = isRevoked,
				ReplacedByTokenHash = replacedByTokenHash,
				CreatedAt = createdAt,
				CreatedByIp = table.Column<string>("character varying(45)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_RefreshTokens", x => (object)x.Id);
			table.ForeignKey("FK_RefreshTokens_AspNetUsers_UserId", x => (object)x.UserId, "AspNetUsers", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null);
		migrationBuilder.CreateTable("Recipes", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> authorId = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> categoryId = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 200;
			OperationBuilder<AddColumnOperation> title = table.Column<string>("character varying(200)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 220;
			OperationBuilder<AddColumnOperation> slug = table.Column<string>("character varying(220)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> description = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> instructions = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> prepTimeMinutes = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> cookTimeMinutes = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> servings = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> publishedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> difficulty = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> status = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 8;
			int? num2 = 2;
			OperationBuilder<AddColumnOperation> nutrition_Calories = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num, num2, (bool?)null);
			num2 = 8;
			num = 2;
			OperationBuilder<AddColumnOperation> nutrition_Protein = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num2, num, (bool?)null);
			num = 8;
			num2 = 2;
			OperationBuilder<AddColumnOperation> nutrition_Carbohydrates = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num, num2, (bool?)null);
			num2 = 8;
			num = 2;
			OperationBuilder<AddColumnOperation> nutrition_Fat = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num2, num, (bool?)null);
			num = 8;
			num2 = 2;
			OperationBuilder<AddColumnOperation> nutrition_Fiber = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num, num2, (bool?)null);
			num2 = 8;
			num = 2;
			return new
			{
				Id = id,
				AuthorId = authorId,
				CategoryId = categoryId,
				Title = title,
				Slug = slug,
				Description = description,
				Instructions = instructions,
				PrepTimeMinutes = prepTimeMinutes,
				CookTimeMinutes = cookTimeMinutes,
				Servings = servings,
				PublishedAt = publishedAt,
				Difficulty = difficulty,
				Status = status,
				Nutrition_Calories = nutrition_Calories,
				Nutrition_Protein = nutrition_Protein,
				Nutrition_Carbohydrates = nutrition_Carbohydrates,
				Nutrition_Fat = nutrition_Fat,
				Nutrition_Fiber = nutrition_Fiber,
				Nutrition_Sodium = table.Column<decimal>("numeric(8,2)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num2, num, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				UpdatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsDeleted = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				RowVersion = table.Column<byte[]>("bytea", (bool?)null, (int?)null, true, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_Recipes", x => (object)x.Id);
			table.ForeignKey("FK_Recipes_AspNetUsers_AuthorId", x => (object)x.AuthorId, "AspNetUsers", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)1);
			table.ForeignKey("FK_Recipes_Categories_CategoryId", x => (object)x.CategoryId, "Categories", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)1);
		}, (string)null);
		migrationBuilder.CreateTable("RecipeImages", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> recipeId = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 500;
			OperationBuilder<AddColumnOperation> originalUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 500;
			OperationBuilder<AddColumnOperation> mediumUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 500;
			OperationBuilder<AddColumnOperation> thumbnailUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 200;
			return new
			{
				Id = id,
				RecipeId = recipeId,
				OriginalUrl = originalUrl,
				MediumUrl = mediumUrl,
				ThumbnailUrl = thumbnailUrl,
				AltText = table.Column<string>("character varying(200)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsPrimary = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				OrderIndex = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				UpdatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsDeleted = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				RowVersion = table.Column<byte[]>("bytea", (bool?)null, (int?)null, true, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_RecipeImages", x => (object)x.Id);
			table.ForeignKey("FK_RecipeImages_Recipes_RecipeId", x => (object)x.RecipeId, "Recipes", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null);
		migrationBuilder.CreateTable("RecipeIngredients", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> recipeId = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 200;
			OperationBuilder<AddColumnOperation> name = table.Column<string>("character varying(200)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 10;
			int? num2 = 3;
			OperationBuilder<AddColumnOperation> quantity = table.Column<decimal>("numeric(10,3)", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, num, num2, (bool?)null);
			num2 = 50;
			OperationBuilder<AddColumnOperation> unit = table.Column<string>("character varying(50)", (bool?)null, num2, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num2 = 500;
			return new
			{
				Id = id,
				RecipeId = recipeId,
				Name = name,
				Quantity = quantity,
				Unit = unit,
				Notes = table.Column<string>("character varying(500)", (bool?)null, num2, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				OrderIndex = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				UpdatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsDeleted = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				RowVersion = table.Column<byte[]>("bytea", (bool?)null, (int?)null, true, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_RecipeIngredients", x => (object)x.Id);
			table.ForeignKey("FK_RecipeIngredients_Recipes_RecipeId", x => (object)x.RecipeId, "Recipes", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null);
		migrationBuilder.CreateTable("RecipeSteps", (ColumnsBuilder table) =>
		{
			OperationBuilder<AddColumnOperation> id = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> recipeId = table.Column<Guid>("uuid", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> stepNumber = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			int? num = 200;
			OperationBuilder<AddColumnOperation> title = table.Column<string>("character varying(200)", (bool?)null, num, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> description = table.Column<string>("text", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			OperationBuilder<AddColumnOperation> durationMinutes = table.Column<int>("integer", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null);
			num = 500;
			return new
			{
				Id = id,
				RecipeId = recipeId,
				StepNumber = stepNumber,
				Title = title,
				Description = description,
				DurationMinutes = durationMinutes,
				ImageUrl = table.Column<string>("character varying(500)", (bool?)null, num, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				CreatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				UpdatedAt = table.Column<DateTime>("timestamp with time zone", (bool?)null, (int?)null, false, (string)null, true, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				IsDeleted = table.Column<bool>("boolean", (bool?)null, (int?)null, false, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null),
				RowVersion = table.Column<byte[]>("bytea", (bool?)null, (int?)null, true, (string)null, false, (object)null, (string)null, (string)null, (bool?)null, (string)null, (string)null, (int?)null, (int?)null, (bool?)null)
			};
		}, (string)null, table =>
		{
			table.PrimaryKey("PK_RecipeSteps", x => (object)x.Id);
			table.ForeignKey("FK_RecipeSteps_Recipes_RecipeId", x => (object)x.RecipeId, "Recipes", "Id", (string)null, (ReferentialAction)0, (ReferentialAction)2);
		}, (string)null);
		migrationBuilder.CreateIndex("IX_AspNetUsers_Email", "AspNetUsers", "Email", (string)null, true, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_AspNetUsers_UserName", "AspNetUsers", "UserName", (string)null, true, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_Categories_Slug", "Categories", "Slug", (string)null, true, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_RecipeImages_RecipeId", "RecipeImages", "RecipeId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_RecipeIngredients_RecipeId", "RecipeIngredients", "RecipeId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_Recipes_AuthorId", "Recipes", "AuthorId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_Recipes_CategoryId", "Recipes", "CategoryId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_Recipes_Slug", "Recipes", "Slug", (string)null, true, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_Recipes_Status", "Recipes", "Status", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_RecipeSteps_RecipeId", "RecipeSteps", "RecipeId", (string)null, false, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_RefreshTokens_TokenHash", "RefreshTokens", "TokenHash", (string)null, true, (string)null, (bool[])null);
		migrationBuilder.CreateIndex("IX_RefreshTokens_UserId", "RefreshTokens", "UserId", (string)null, false, (string)null, (bool[])null);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropTable("RecipeImages", (string)null);
		migrationBuilder.DropTable("RecipeIngredients", (string)null);
		migrationBuilder.DropTable("RecipeSteps", (string)null);
		migrationBuilder.DropTable("RefreshTokens", (string)null);
		migrationBuilder.DropTable("Recipes", (string)null);
		migrationBuilder.DropTable("AspNetUsers", (string)null);
		migrationBuilder.DropTable("Categories", (string)null);
	}

	protected override void BuildTargetModel(ModelBuilder modelBuilder)
	{
		modelBuilder.HasAnnotation("ProductVersion", (object)"10.0.0").HasAnnotation("Relational:MaxIdentifierLength", (object)63);
		NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.ApplicationUser", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Id"), "text");
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
			RelationalPropertyBuilderExtensions.HasColumnName<byte[]>(RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true)
				.ValueGeneratedOnAddOrUpdate(), "bytea"), "RowVersion");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Slug").IsRequired(true).HasMaxLength(120), "character varying(120)");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("UpdatedAt"), "timestamp with time zone");
			b.HasKey(new string[1] { "Id" });
			b.HasIndex(new string[1] { "Slug" }).IsUnique(true);
			RelationalEntityTypeBuilderExtensions.ToTable(b, "Categories");
		}));
		modelBuilder.Entity("CulinaryBlog.Domain.Entities.Recipe", (Action<EntityTypeBuilder>)((EntityTypeBuilder b) =>
		{
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("Id").ValueGeneratedOnAdd(), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("AuthorId").IsRequired(true), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<Guid>(b.Property<Guid>("CategoryId"), "uuid");
			RelationalPropertyBuilderExtensions.HasColumnName<string>(RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Content").IsRequired(true), "text"), "Instructions");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("CookTimeMinutes"), "integer"), "CookTimeMinutes");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime>(b.Property<DateTime>("CreatedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("Description").IsRequired(true), "text");
			RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("Difficulty"), "integer");
			RelationalPropertyBuilderExtensions.HasColumnType<bool>(b.Property<bool>("IsDeleted"), "boolean");
			RelationalPropertyBuilderExtensions.HasColumnName<int>(RelationalPropertyBuilderExtensions.HasColumnType<int>(b.Property<int>("PrepTimeMinutes"), "integer"), "PrepTimeMinutes");
			RelationalPropertyBuilderExtensions.HasColumnType<DateTime?>(b.Property<DateTime?>("PublishedAt"), "timestamp with time zone");
			RelationalPropertyBuilderExtensions.HasColumnName<byte[]>(RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true)
				.ValueGeneratedOnAddOrUpdate(), "bytea"), "RowVersion");
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
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true)
				.ValueGeneratedOnAddOrUpdate(), "bytea");
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
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true)
				.ValueGeneratedOnAddOrUpdate(), "bytea");
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
			RelationalPropertyBuilderExtensions.HasColumnType<byte[]>(b.Property<byte[]>("RowVersion").IsConcurrencyToken(true).IsRequired(true)
				.ValueGeneratedOnAddOrUpdate(), "bytea");
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
			RelationalPropertyBuilderExtensions.HasColumnType<string>(b.Property<string>("UserId").IsRequired(true), "text");
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
