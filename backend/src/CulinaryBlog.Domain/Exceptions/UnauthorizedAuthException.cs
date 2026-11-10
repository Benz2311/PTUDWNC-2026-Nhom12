using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class UnauthorizedAuthException : BaseDomainException
{
    public UnauthorizedAuthException(string message = "Unauthorized.", string errorCode = "UNAUTHORIZED")
        : base(message, HttpStatusCode.Unauthorized, errorCode)
    {
    }
}