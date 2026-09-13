
using CulinaryBlog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    database = "configured",
    timestamp = DateTime.UtcNow
}));

app.MapGet("/", () => Results.Ok(new
{
    service = "CulinaryBlog API",
    status = "running",
    database = "PostgreSQL"
}));

app.Run();
