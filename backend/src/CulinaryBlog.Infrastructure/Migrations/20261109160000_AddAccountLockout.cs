using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109160000_AddAccountLockout")]
public sealed class AddAccountLockout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AccessFailedCount", table: "Users", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<DateTime>(
            name: "LockoutEnd", table: "Users", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LockoutEnd", table: "Users");
        migrationBuilder.DropColumn(name: "AccessFailedCount", table: "Users");
    }
}
