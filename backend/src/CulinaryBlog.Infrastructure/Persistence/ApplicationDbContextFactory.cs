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
            ?? "localhost";

        var port =
            Environment.GetEnvironmentVariable("POSTGRES_PORT")
            ?? "5432";

        var database =
            Environment.GetEnvironmentVariable("POSTGRES_DB")
            ?? "culinary_blog";

        var username =
            Environment.GetEnvironmentVariable("POSTGRES_USER")
            ?? "postgres";

        var password =
            Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
            ?? "Manh80891234567";

        var connectionString =
            $"Host={host};" +
            $"Port={port};" +
            $"Database={database};" +
            $"Username={username};" +
            $"Password={password};" +
            "Include Error Detail=true";

        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        return new ApplicationDbContext(
            optionsBuilder.Options);
    }
}