using CulinaryBlog.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>
/// Email service dùng MailKit theo SRS — SMTP/email service.
/// </summary>
public sealed class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IConfiguration configuration, ILogger<MailKitEmailService> logger)
    {
        _logger = logger;
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

        var socketOptions = _settings.UseSsl
            ? (_settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto)
            : SecureSocketOptions.None;

        try
        {
            _logger.LogInformation(
                "Connecting to SMTP server {Host}:{Port} with options {Options} for recipient {To}",
                _settings.Host,
                _settings.Port,
                socketOptions,
                message.To);

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, ct);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
            }

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("Email sent successfully to {To}", message.To);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP delivery failed for {To} via {Host}:{Port}", message.To, _settings.Host, _settings.Port);
            throw;
        }
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
