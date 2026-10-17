using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class ConflictException : BaseDomainException
{
    public ConflictException(string message = "Resource already exists.")
        : base(message, HttpStatusCode.Conflict, "RESOURCE_ALREADY_EXISTS")
    {
    }
}