namespace CulinaryBlog.Application.DTOs.Auth;

public class LoginRequest
{
    public string UserNameOrEmail { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string Password { get; set; } = string.Empty;
}
