using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class ForbiddenException : BaseDomainException
{
    public ForbiddenException(string message = "Access denied.")
        : base(message, HttpStatusCode.Forbidden, "FORBIDDEN")
    {
    }
}