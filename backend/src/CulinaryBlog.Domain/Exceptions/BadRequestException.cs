using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class BadRequestException : BaseDomainException
{
    public BadRequestException(string message = "Bad request.")
        : base(message, HttpStatusCode.BadRequest, "BAD_REQUEST")
    {
    }
}