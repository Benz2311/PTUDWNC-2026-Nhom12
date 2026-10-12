using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class ConcurrencyConflictException : BaseDomainException
{
    public ConcurrencyConflictException()
        : base("Dữ liệu đã bị thay đổi bởi người dùng khác, vui lòng tải lại trang.", HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT")
    {
    }

    public ConcurrencyConflictException(string entityName, object entityId)
        : base($"{entityName} với ID '{entityId}' đã bị thay đổi bởi người dùng khác, vui lòng tải lại trang.", HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT")
    {
    }

    public ConcurrencyConflictException(string entityName, object entityId, Exception innerException)
        : base($"{entityName} với ID '{entityId}' đã bị thay đổi bởi người dùng khác, vui lòng tải lại trang.", HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT", innerException)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, HttpStatusCode.Conflict, "CONCURRENCY_CONFLICT", innerException)
    {
    }
}
