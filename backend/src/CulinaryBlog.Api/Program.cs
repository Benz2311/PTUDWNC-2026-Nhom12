using CulinaryBlog.Api.Endpoints.Recipes;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Global Exception Handling & RFC 7807 Problem Details (Lab 3)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Đăng ký Application Layer (MediatR Handlers)
builder.Services.AddApplication();

// Đăng ký Infrastructure Layer (PostgreSQL DbContext, Redis Cache, Repositories, Unit of Work)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Kích hoạt Global Exception Handling Middleware trong HTTP Request Pipeline (Lab 3)
app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new { status = "online", service = "CulinaryBlog.Api", version = "1.0.0" }));

// Đăng ký Minimal API Endpoints
app.MapRecipeEndpoints();

app.Run();

// Hỗ trợ WebApplicationFactory trong Integration Tests
public partial class Program { }
