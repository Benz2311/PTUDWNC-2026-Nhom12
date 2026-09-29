using CulinaryBlog.Api.Endpoints.Recipes;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Application Layer (MediatR Handlers)
builder.Services.AddApplication();

// Đăng ký Infrastructure Layer (PostgreSQL DbContext, Redis Cache)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new { status = "online", service = "CulinaryBlog.Api", version = "1.0.0" }));

// Đăng ký Minimal API Endpoints
app.MapRecipeEndpoints();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
