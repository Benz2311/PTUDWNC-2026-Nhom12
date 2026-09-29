using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CulinaryBlog.Infrastructure.Persistence;

public class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var host =
            Environment.GetEnvironmentVariable("POSTGRES_HOST")
            ?? "127.0.0.1";

        var port =
            Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";

        var database =
            Environment.GetEnvironmentVariable("POSTGRES_DB")
            ?? "culinaryblog_db";

        var username =
            Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? "postgres";

        var password =
            Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? "YourStrongPassword123";

        var connectionString =
            $"Host={host};" +
            $"Port={port};" +
            $"Database={database};" +
            $"Username={username};" +
            $"Password={password}";

        Console.WriteLine($"[DEBUG] Connection string: {connectionString}");

        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(
            optionsBuilder.Options);
    }
}