using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.Extensions;
using CulinaryBlog.Application.Common.Interfaces;
using CulinaryBlog.Application.DTOs;
using CulinaryBlog.Application.Features.Categories.Queries.GetCategories;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CulinaryBlog.IntegrationTests.Api;

public sealed class RateLimitingTests
{
    [Fact]
    public async Task RateLimiting_ExceedingLimit_Returns429TooManyRequests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var sender = new Mock<ISender>();
        var cache = new Mock<ICacheService>();

        IReadOnlyList<CategoryDto> categories =
            [new(Guid.NewGuid(), "Mon Chay", "mon-chay", null, null, 1, 5)];

        cache.Setup(c => c.GetAsync<IReadOnlyList<CategoryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);
        sender.Setup(s => s.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        builder.Services.AddSingleton(sender.Object);
        builder.Services.AddSingleton(cache.Object);
        builder.Services.AddAuthorization();

        // Cấu hình RateLimiter với PermitLimit = 2 cho kịch bản test vượt giới hạn
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Đã vượt quá giới hạn số lượng yêu cầu cho phép. Vui lòng thử lại sau.",
                };
                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: cancellationToken);
            };

            options.AddFixedWindowLimiter(
                RateLimitingExtensions.PolicyName,
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = 2;
                    limiterOptions.Window = TimeSpan.FromMinutes(1);
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = 0;
                });
        });

        var app = builder.Build();
        app.UseRateLimiter();
        app.MapCategoryEndpoints();
        await app.StartAsync();

        var client = app.GetTestClient();

        // Request 1: Thành công
        var response1 = await client.GetAsync("/api/v1/categories/");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Request 2: Thành công
        var response2 = await client.GetAsync("/api/v1/categories/");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Request 3: Vượt quá giới hạn (limit = 2) -> trả về 429 Too Many Requests
        var response3 = await client.GetAsync("/api/v1/categories/");
        response3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var content = await response3.Content.ReadAsStringAsync();
        content.Should().Contain("Too Many Requests");
        content.Should().Contain("429");
    }
}
