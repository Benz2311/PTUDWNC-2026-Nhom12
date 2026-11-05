using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261104120000_AddPhaseFourSocialNutrition")]
public partial class AddPhaseFourSocialNutrition : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "Consumed",
            table: "MealPlanItems",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ConsumedAt",
            table: "MealPlanItems",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ActivityLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ActivityType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ActivityLogs_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserFollows",
            columns: table => new
            {
                FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                FollowingId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFollows", x => new { x.FollowerId, x.FollowingId });
                table.CheckConstraint("CK_UserFollows_NoSelfFollow", "\"FollowerId\" <> \"FollowingId\"");
                table.ForeignKey(
                    name: "FK_UserFollows_AspNetUsers_FollowerId",
                    column: x => x.FollowerId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserFollows_AspNetUsers_FollowingId",
                    column: x => x.FollowingId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserNutritionGoals",
            columns: table => new
            {
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Age = table.Column<int>(type: "integer", nullable: false),
                Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                HeightCm = table.Column<double>(type: "double precision", precision: 6, scale: 2, nullable: false),
                WeightKg = table.Column<double>(type: "double precision", precision: 6, scale: 2, nullable: false),
                ActivityLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                GoalType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                TargetCalories = table.Column<double>(type: "double precision", precision: 8, scale: 2, nullable: false),
                TargetProteinG = table.Column<double>(type: "double precision", precision: 8, scale: 2, nullable: false),
                TargetCarbG = table.Column<double>(type: "double precision", precision: 8, scale: 2, nullable: false),
                TargetFatG = table.Column<double>(type: "double precision", precision: 8, scale: 2, nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserNutritionGoals", x => x.UserId);
                table.ForeignKey(
                    name: "FK_UserNutritionGoals_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ActivityLogs_UserId_CreatedAt",
            table: "ActivityLogs",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_ActivityLogs_TargetId",
            table: "ActivityLogs",
            column: "TargetId");

        migrationBuilder.CreateIndex(
            name: "IX_UserFollows_FollowingId_CreatedAt",
            table: "UserFollows",
            columns: new[] { "FollowingId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_MealPlanItems_MealPlanId_ConsumedAt",
            table: "MealPlanItems",
            columns: new[] { "MealPlanId", "ConsumedAt" },
            filter: "\"Consumed\" = true");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ActivityLogs");
        migrationBuilder.DropTable(name: "UserFollows");
        migrationBuilder.DropTable(name: "UserNutritionGoals");
        migrationBuilder.DropIndex(name: "IX_MealPlanItems_MealPlanId_ConsumedAt", table: "MealPlanItems");
        migrationBuilder.DropColumn(name: "Consumed", table: "MealPlanItems");
        migrationBuilder.DropColumn(name: "ConsumedAt", table: "MealPlanItems");
    }
}
