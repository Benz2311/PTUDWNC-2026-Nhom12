using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Api.Endpoints.Auth;

public static class EmailConfirmationTokens
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public static string Create(Guid userId, string email, IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"] ?? "CulinaryBlog",
            audience: configuration["Jwt:Audience"] ?? "CulinaryBlogClient",
            claims:
            [
                new Claim("email_confirmation_user_id", userId.ToString()),
                new Claim("email_confirmation_email", email),
                new Claim("purpose", "email_confirmation"),
            ],
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(Lifetime),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static bool TryValidate(
        string token,
        IConfiguration configuration,
        out Guid userId,
        out string email)
    {
        userId = Guid.Empty;
        email = string.Empty;
        var signingKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "CulinaryBlog",
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"] ?? "CulinaryBlogClient",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out _);

            var tokenEmail = principal.FindFirstValue("email_confirmation_email");
            if (principal.FindFirstValue("purpose") != "email_confirmation" ||
                !Guid.TryParse(principal.FindFirstValue("email_confirmation_user_id"), out userId) ||
                string.IsNullOrWhiteSpace(tokenEmail))
            {
                return false;
            }

            email = tokenEmail;
            return true;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}