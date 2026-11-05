using Microsoft.AspNetCore.Builder;

namespace CulinaryBlog.Api.Middleware;

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder application)
    {
        return application.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
