namespace CulinaryBlog.Api.Contracts;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Success";
    public T Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse(T data, string? message = null)
    {
        Data = data;
        if (!string.IsNullOrWhiteSpace(message))
        {
            Message = message;
        }
    }
}
