using CulinaryBlog.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — đăng ký handlers từ assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline: LoggingBehavior bọc ngoài ghi nhận thời gian/lỗi, sau đó đến ValidationBehavior
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — tự scan toàn bộ validators trong assembly
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<Interfaces.IRecipeWriteService, Features.Recipes.RecipeWriteService>();

        return services;
    }
}