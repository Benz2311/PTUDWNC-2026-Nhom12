using Bogus;
using CulinaryBlog.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence.Seed;

public static class IdentityDataSeeder
{
    public const string AdminRoleName = "Admin";
    public const string AuthorRoleName = "Author";
    public const string DefaultPassword = "Password123!";
    public const string PrimaryAuthorId =
        "11111111-1111-1111-1111-111111111111";

    private const string AdminRoleId =
        "00000000-0000-0000-0000-000000000001";

    private const string AuthorRoleId =
        "00000000-0000-0000-0000-000000000002";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher<ApplicationUser> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        var roleIds = await EnsureRolesAsync(
            context,
            cancellationToken);

        foreach (var account in CreateAccounts())
        {
            var normalizedUserName =
                account.UserName.ToUpperInvariant();

            var user = await context.Users.SingleOrDefaultAsync(
                item => item.NormalizedUserName == normalizedUserName,
                cancellationToken);

            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = account.Id,
                    UserName = account.UserName,
                    NormalizedUserName = normalizedUserName,
                    Email = account.Email,
                    NormalizedEmail = account.Email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    DisplayName = account.DisplayName,
                    AvatarUrl = account.AvatarUrl,
                    Bio = account.Bio,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                user.PasswordHash = passwordHasher.HashPassword(
                    user,
                    DefaultPassword);

                context.Users.Add(user);
            }

            var roleId = roleIds[account.RoleName];
            var hasRole = await context.UserRoles.AnyAsync(
                item => item.UserId == user.Id && item.RoleId == roleId,
                cancellationToken);

            if (!hasRole)
            {
                context.UserRoles.Add(new IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = roleId
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, string>>
        EnsureRolesAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new RoleDefinition(AdminRoleId, AdminRoleName),
            new RoleDefinition(AuthorRoleId, AuthorRoleName)
        };

        foreach (var definition in definitions)
        {
            var normalizedName = definition.Name.ToUpperInvariant();
            var role = await context.Roles.SingleOrDefaultAsync(
                item => item.NormalizedName == normalizedName,
                cancellationToken);

            if (role is null)
            {
                context.Roles.Add(new IdentityRole
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    NormalizedName = normalizedName,
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return await context.Roles
            .Where(role => role.Name == AdminRoleName || role.Name == AuthorRoleName)
            .ToDictionaryAsync(
                role => role.Name!,
                role => role.Id,
                cancellationToken);
    }

    private static IReadOnlyCollection<SeedAccount> CreateAccounts()
    {
        var faker = new Faker("vi")
        {
            Random = new Randomizer(20260929)
        };

        var authors = Enumerable.Range(1, 5)
            .Select(index =>
            {
                var userName = $"author{index}";
                var displayName = faker.Name.FullName();

                return new SeedAccount(
                    index == 1
                        ? PrimaryAuthorId
                        : $"11111111-1111-1111-1111-{index:D12}",
                    userName,
                    $"{userName}@culinaryblog.local",
                    displayName,
                    $"https://api.dicebear.com/9.x/initials/svg?seed={userName}",
                    faker.Lorem.Sentence(),
                    AuthorRoleName);
            });

        return authors
            .Append(
                new SeedAccount(
                    "00000000-0000-0000-0000-000000000010",
                    "admin",
                    "admin@culinaryblog.local",
                    "Culinary Blog Admin",
                    "https://api.dicebear.com/9.x/initials/svg?seed=admin",
                    "Quản trị viên hệ thống Culinary Blog.",
                    AdminRoleName))
            .ToArray();
    }

    private sealed record RoleDefinition(string Id, string Name);

    private sealed record SeedAccount(
        string Id,
        string UserName,
        string Email,
        string DisplayName,
        string AvatarUrl,
        string Bio,
        string RoleName);
}
