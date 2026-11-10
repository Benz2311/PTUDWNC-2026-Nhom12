using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261109130000_AddSessionVersion")]
public sealed class AddSessionVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SessionVersion", table: "Users", type: "uuid", nullable: false, defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<Guid>(
            name: "SessionVersion", table: "RefreshTokens", type: "uuid", nullable: false, defaultValue: Guid.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SessionVersion", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "SessionVersion", table: "Users");
    }
}
