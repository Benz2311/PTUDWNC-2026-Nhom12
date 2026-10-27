namespace CulinaryBlog.Application.Exceptions;

/// <summary>
/// Ngoại lệ cơ sở cho các lỗi liên quan đến dịch vụ lưu trữ tệp (File Storage / MinIO).
/// </summary>
public class StorageException : Exception
{
    public StorageException(string message) : base(message)
    {
    }

    public StorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Ngoại lệ đại diện cho tình trạng dịch vụ lưu trữ (MinIO / S3) tạm thời không thể kết nối hoặc bị sập.
/// Ngoại lệ này được ánh xạ thành mã lỗi HTTP 503 (Service Unavailable) ở tầng API.
/// </summary>
public class StorageUnavailableException : StorageException
{
    public StorageUnavailableException(string message = "Dịch vụ lưu trữ tệp (Object Storage) tạm thời không khả dụng. Vui lòng thử lại sau.")
        : base(message)
    {
    }

    public StorageUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
