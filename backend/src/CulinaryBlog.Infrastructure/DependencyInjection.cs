using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Cấu hình PostgreSQL DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var host = configuration["POSTGRES_HOST"] ?? "localhost";
            var port = configuration["POSTGRES_PORT"] ?? "5432";
            var database = configuration["POSTGRES_DB"] ?? "culinary_blog";
            var username = configuration["POSTGRES_USER"] ?? "postgres";
            var password = configuration["POSTGRES_PASSWORD"] ?? string.Empty;

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // 2. Cấu hình Caching (Redis hoặc In-Memory Fallback)
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["REDIS_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "CulinaryBlog:";
            });
        }
        else
        {
            // Fallback In-Memory Distributed Cache khi môi trường dev không cấu hình Redis
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, ResilientCacheService>();

        // 3. Đăng ký Repository & Unit of Work (Lab 3)
        services.AddScoped(typeof(IRepository<>), typeof(Persistence.Repositories.Repository<>));
        services.AddScoped<CulinaryBlog.Application.Features.Recipes.Interfaces.IRecipeRepository, Persistence.Repositories.RecipeRepository>();
        services.AddScoped<IUnitOfWork, Persistence.UnitOfWork.UnitOfWork>();

        return services;
    }
}
