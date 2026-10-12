using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class UnauthorizedAuthException : BaseDomainException
{
    public UnauthorizedAuthException(string message = "Unauthorized.")
        : base(message, HttpStatusCode.Unauthorized, "UNAUTHORIZED")
    {
    }
}