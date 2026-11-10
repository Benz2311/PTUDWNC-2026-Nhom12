namespace CulinaryBlog.Application.DTOs.Auth;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? Nonce { get; set; }
}