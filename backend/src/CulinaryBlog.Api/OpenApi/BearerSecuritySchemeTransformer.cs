using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CulinaryBlog.Api.OpenApi;

/// <summary>
/// Thêm JWT Bearer security scheme vào OpenAPI document để Swagger/Scalar
/// hỗ trợ nhập Authorization: Bearer {token}.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Bearer Token. Nhập theo định dạng: {access_token}"
        };

        // Áp dụng Bearer auth toàn cục cho mọi endpoint
        var schemeReference = new OpenApiSecuritySchemeReference(SchemeName, document, null);
        document.Security.Add(new OpenApiSecurityRequirement
        {
            { schemeReference, new List<string>() }
        });

        return Task.CompletedTask;
    }
}
