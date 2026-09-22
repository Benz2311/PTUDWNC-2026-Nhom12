
using System.Text;
using System.Text.Json.Serialization;
using CulinaryBlog.Api.Endpoints.Auth;
using CulinaryBlog.Api.Endpoints.Categories;
using CulinaryBlog.Api.Endpoints.Dashboard;
using CulinaryBlog.Api.Endpoints.Files;
using CulinaryBlog.Api.Endpoints.Health;
using CulinaryBlog.Api.Endpoints.Recipes;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
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
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseStaticFiles();

app.MapAuthEndpoints();
app.MapHealthEndpoints();
app.MapCategoryEndpoints();
app.MapRecipeEndpoints();
app.MapDashboardEndpoints();
app.MapFileEndpoints();

app.MapGet("/", () => Results.Ok(new
{
    service = "CulinaryBlog API",
    status = "running",
    database = "PostgreSQL"
}));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await RecipeSeedData.InitializeAsync(db);
}

app.Run();
