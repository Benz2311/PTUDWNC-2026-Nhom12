using System.Net;
using CulinaryBlog.Application.Interfaces;
using CulinaryBlog.Application.Repositories;
using Hangfire;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class WelcomeEmailJob(IUserRepository users, IEmailService email, IConfiguration configuration)
{
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })]
    public async Task RunAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var name = WebUtility.HtmlEncode(user.DisplayName);
        var site = WebUtility.HtmlEncode(configuration["Site:PublicBaseUrl"]);
        var confirmation = user.EmailConfirmed ? "" : "<p>Ki&#7875;m tra email x&#225;c nh&#7853;n t&#224;i kho&#7843;n &#273;&#227; g&#7917;i ri&#234;ng.</p>";
        var body = $"""
            <h1>Ch&#224;o {name},</h1>
            <p>C&#7843;m &#417;n b&#7841;n &#273;&#227; &#273;&#259;ng k&#253; CulinaryBlog.</p>
            {confirmation}
            <p><a href="{site}">CulinaryBlog</a></p>
            """;
        await email.SendAsync(new EmailMessage(user.Email, "Chao mung ban den voi CulinaryBlog!", body), cancellationToken);
    }
}
