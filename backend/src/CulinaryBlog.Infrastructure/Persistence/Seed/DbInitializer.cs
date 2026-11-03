using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();

        logger?.LogInformation("Starting database initialization...");

        // Apply pending migrations
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            logger?.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "DbInitializer migration skipped.");
        }

        // Seed User/Auth accounts
        await SeedUsersAsync(context, passwordHasher, logger, cancellationToken);

        // Seed Categories
        await SeedCategoriesAsync(context, logger, cancellationToken);

        logger?.LogInformation("Database initialization completed.");
    }

    public static async Task SeedUsersAsync(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        var users = new[]
        {
            new SeedUser("admin", "admin@culinaryblog.com", "Admin@123", "Quản trị viên", ["Admin", "Author"], true),
            new SeedUser("author", "author@culinaryblog.com", "Author@123", "Tác giả Demo", ["Author"], true),
            new SeedUser("user", "user@culinaryblog.com", "User@123", "Người dùng Demo", ["User"], true),
            new SeedUser("inactive", "inactive@culinaryblog.com", "Inactive@123", "Tài khoản vô hiệu hóa", ["User"], false)
        };

        foreach (var seed in users)
        {
            if (await context.Users.AnyAsync(u => u.Email == seed.Email, cancellationToken))
            {
                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = seed.UserName,
                Email = seed.Email,
                NormalizedUserName = seed.UserName.ToUpperInvariant(),
                NormalizedEmail = seed.Email.ToUpperInvariant(),
                DisplayName = seed.DisplayName,
                EmailConfirmed = true,
                IsActive = seed.IsActive,
                CreatedAt = DateTime.UtcNow,
                Roles = seed.Roles
            };
            user.PasswordHash = passwordHasher.HashPassword(user, seed.Password);
            await context.Users.AddAsync(user, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("User/Auth seed completed for {Count} accounts.", users.Length);
    }

    private sealed record SeedUser(
        string UserName,
        string Email,
        string Password,
        string DisplayName,
        string[] Roles,
        bool IsActive);

    private static async Task SeedCategoriesAsync(
        ApplicationDbContext context,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var existingCategories = await context.Categories
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        if (existingCategories.Count >= 4)
        {
            logger?.LogDebug("Categories already seeded ({Count} categories).", existingCategories.Count);
            return;
        }

        var categories = new List<Category>
        {
            Category.Create("Món Việt", "mon-viet", "Các món ăn truyền thống Việt Nam", null, 1),
            Category.Create("Món Á", "mon-a", "Các món ăn châu Á", null, 2),
            Category.Create("Món Âu", "mon-au", "Các món ăn châu Âu", null, 3),
            Category.Create("Đồ Uống", "do-uong", "Các loại đồ uống giải khát, đồ uống có cồn", null, 4)
        };

        await context.Categories.AddRangeAsync(categories, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        logger?.LogInformation("Seeded {Count} categories.", categories.Count);
    }
}