using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapOpenApi();

app.MapScalarApiReference();

app.UseCors("Frontend");

app.MapCategoryEndpoints();

app.Run();