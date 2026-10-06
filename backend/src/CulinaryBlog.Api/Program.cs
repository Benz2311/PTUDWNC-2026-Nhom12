
using System.Text;
using System.Text.Json.Serialization;
using CulinaryBlog.Api.Endpoints.Auth;
using CulinaryBlog.Api.Endpoints.Categories;
using CulinaryBlog.Api.Endpoints.Dashboard;
using CulinaryBlog.Api.Endpoints.Files;
using CulinaryBlog.Api.Endpoints.Health;
using CulinaryBlog.Api.Endpoints.Recipes;
using CulinaryBlog.Api.Endpoints.Sitemap;
using CulinaryBlog.Api.Jobs;
using CulinaryBlog.Api.Contracts;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters.");
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CulinaryBlog";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CulinaryBlogClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var corsOrigins = (builder.Configuration["Cors:Origins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins)
        .WithExposedHeaders("ETag")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<SitemapGenerationHostedService>();
builder.Services.AddHostedService<MinioBucketPolicyHostedService>();
if (builder.Configuration.GetValue<bool>("Recipes:PurgeJobEnabled"))
{
    builder.Services.AddHostedService<RecipePurgeHostedService>();
}

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (exception is DbUpdateConcurrencyException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Concurrency conflict",
            detail: "The resource was changed by another request. Reload and try again.",
            type: "RECIPE_CONCURRENCY_CONFLICT").ExecuteAsync(context);
        return;
    }

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Internal server error",
        detail: "An unexpected error occurred.",
        type: "INTERNAL_SERVER_ERROR").ExecuteAsync(context);
}));
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapAuthEndpoints();
app.MapHealthEndpoints();
app.MapCategoryEndpoints();
app.MapRecipeEndpoints();
app.MapSitemapEndpoints();
app.MapDashboardEndpoints();
app.MapFileEndpoints();

app.MapGet("/", () => Results.Ok(new ApiResponse<object>(new
{
    service = "CulinaryBlog API",
    status = "running",
    database = "PostgreSQL"
})));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await RecipeSeedData.InitializeAsync(db, app.Configuration);
}

app.Run();
