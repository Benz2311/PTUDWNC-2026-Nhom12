namespace CulinaryBlog.Application.DTOs.Auth;

public class ConfirmEmailRequest
{
    public string Token { get; set; } = string.Empty;
}