namespace CulinaryBlog.Application.Interfaces;

public record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);

/// <summary>Email service interface — implemented by MailKitEmailService in Infrastructure.</summary>
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
