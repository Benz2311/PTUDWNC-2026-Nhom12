using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Authentication;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuthenticationService, JwtTokenService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IUnitOfWork, ApplicationUnitOfWork>();
        services.AddScoped<IRecipeCacheInvalidator, RecipeCacheInvalidator>();
        services.AddScoped<IRecipePurgeService, RecipePurgeService>();
        services.AddScoped<ISitemapGenerator, SitemapGenerator>();

        var redisConnection = configuration["Redis:ConnectionString"] ?? "localhost:6380";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        var minioEndpoint = configuration["Minio:Endpoint"]
            ?? throw new InvalidOperationException("Minio:Endpoint is not configured.");
        var minioUri = new Uri(minioEndpoint);
        var minioClient = new MinioClient()
            .WithEndpoint(minioUri.Host, minioUri.Port)
            .WithCredentials(
                configuration["Minio:AccessKey"] ?? throw new InvalidOperationException("Minio:AccessKey is not configured."),
                configuration["Minio:SecretKey"] ?? throw new InvalidOperationException("Minio:SecretKey is not configured."))
            .WithSSL(minioUri.Scheme == Uri.UriSchemeHttps)
            .Build();
        services.AddSingleton<IMinioClient>(minioClient);

        return services;
    }
}
