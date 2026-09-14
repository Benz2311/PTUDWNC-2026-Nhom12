using CulinaryBlog.Application.Contracts.Persistence;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Repositories;
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
        services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseNpgsql(
                    configuration.GetConnectionString(
                        "DefaultConnection")));

        services.AddScoped<ICategoryRepository,
            CategoryRepository>();

        return services;
    }
}