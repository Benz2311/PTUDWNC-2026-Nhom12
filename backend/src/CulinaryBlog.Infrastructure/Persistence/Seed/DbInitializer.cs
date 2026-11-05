using CulinaryBlog.Domain.Entities;
using CulinaryBlog.Domain.Enums;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

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

        // Seed Admin User
        await SeedAdminUserAsync(context, passwordHasher, configuration, logger, cancellationToken);

        // Seed Categories
        await SeedCategoriesAsync(context, logger, cancellationToken);

        logger?.LogInformation("Database initialization completed.");
    }

    private static async Task SeedAdminUserAsync(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConfiguration configuration,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var adminEmail = configuration["Seed:AdminEmail"];
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger?.LogInformation("Admin seed skipped because Seed:AdminEmail and Seed:AdminPassword are not configured.");
            return;
        }

        if (!Regex.IsMatch(adminEmail, @"^[0-9]+@dlu\.edu\.vn$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("Seed:AdminEmail must use the format <student-id>@dlu.edu.vn.");
        }

        if (adminPassword.Length < 12)
        {
            throw new InvalidOperationException("Seed:AdminPassword must contain at least 12 characters.");
        }

        var existingAdmin = await context.Users.FirstOrDefaultAsync(
            user => user.Email.ToLower() == adminEmail.ToLower(),
            cancellationToken);
        if (existingAdmin is not null)
        {
            logger?.LogInformation("Admin user already exists: {Email}", adminEmail);
            return;
        }

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminEmail[..adminEmail.IndexOf('@')],
            Email = adminEmail,
            DisplayName = "Administrator",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = ["Admin", "Author"]
        };
        admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);

        await context.Users.AddAsync(admin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger?.LogInformation("Created admin user: {Email}", adminEmail);
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