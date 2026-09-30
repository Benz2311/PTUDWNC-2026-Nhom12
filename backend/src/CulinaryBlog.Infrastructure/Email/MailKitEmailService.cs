using CulinaryBlog.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>
/// Email service dùng MailKit theo SRS — SMTP/email service.
/// </summary>
public sealed class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public MailKitEmailService(IConfiguration configuration)
    {
        _settings = configuration
            .GetSection(EmailSettings.Section)
            .Get<EmailSettings>()
            ?? throw new InvalidOperationException("Email settings are not configured.");
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody
        };

        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.Host,
            _settings.Port,
            _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            await client.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
        }

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}

public sealed class EmailSettings
{
    public const string Section = "Email";

    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = string.Empty;
    public string SenderName { get; init; } = "CulinaryBlog";
}
