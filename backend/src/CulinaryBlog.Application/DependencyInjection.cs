using CulinaryBlog.Application.Behaviors;
using CulinaryBlog.Application.Features.Categories;
using CulinaryBlog.Application.Features.Recipes;
using CulinaryBlog.Application.Interfaces;
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

            // Pipeline: ValidationBehavior chạy trước mọi handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // FluentValidation — tự scan toàn bộ validators trong assembly
        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<IRecipeWriteService, Features.Recipes.RecipeWriteService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IRecipeService, RecipeService>();

        return services;
    }
}