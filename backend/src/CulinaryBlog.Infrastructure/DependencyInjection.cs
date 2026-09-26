using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Features.Recipes;
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
        var connectionString = RequireSetting(configuration, "ConnectionStrings:DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        var redisConnection = configuration["Redis:ConnectionString"] ?? "localhost:6380";
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        var minioEndpoint = RequireSetting(configuration, "Minio:Endpoint");
        var minioUri = new Uri(minioEndpoint);
        var minioClient = new MinioClient()
            .WithEndpoint(minioUri.Host, minioUri.Port)
            .WithCredentials(
                RequireSetting(configuration, "Minio:AccessKey"),
                RequireSetting(configuration, "Minio:SecretKey"))
            .WithSSL(minioUri.Scheme == Uri.UriSchemeHttps)
            .Build();
        services.AddSingleton<IMinioClient>(minioClient);

        services.AddScoped<IAuthenticationService, JwtTokenService>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IRecipeWriteService, RecipeWriteService>();
        services.AddScoped<IUnitOfWork, ApplicationUnitOfWork>();

        return services;
    }

    private static string RequireSetting(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"{key} is not configured.");
}
