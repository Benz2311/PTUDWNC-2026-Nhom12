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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        logger?.LogInformation("Starting database initialization...");

        // Apply pending migrations
        await context.Database.MigrateAsync(cancellationToken);
        logger?.LogInformation("Database migrations applied.");

        // Seed Admin User
        await SeedAdminUserAsync(userManager, logger, cancellationToken);

        // Seed Categories
        await SeedCategoriesAsync(context, logger, cancellationToken);

        logger?.LogInformation("Database initialization completed.");
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        const string adminEmail = "admin@culinaryblog.com";
        const string adminUserName = "admin";
        const string adminPassword = "Admin@123";

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is not null)
        {
            logger?.LogInformation("Admin user already exists: {Email}", adminEmail);
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminUserName,
            Email = adminEmail,
            DisplayName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = ["Admin", "Author"]
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            logger?.LogInformation("Created admin user: {Email}", adminEmail);
        }
        else
        {
            logger?.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

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