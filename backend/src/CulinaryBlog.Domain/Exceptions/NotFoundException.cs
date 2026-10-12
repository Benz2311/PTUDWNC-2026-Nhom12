using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class NotFoundException : BaseDomainException
{
    public NotFoundException(string message = "Resource not found.")
        : base(message, HttpStatusCode.NotFound, "NOT_FOUND")
    {
    }

    public NotFoundException(string entityName, object entityId)
        : base($"{entityName} with ID '{entityId}' was not found.", HttpStatusCode.NotFound, "NOT_FOUND")
    {
    }
}