using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Infrastructure.Authentication;

namespace CulinaryBlog.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, JwtTokenService>();
        return services;
    }
}
