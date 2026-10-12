using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class InvalidBusinessRuleException : BaseDomainException
{
    public InvalidBusinessRuleException(string message = "Invalid business rule.")
        : base(message, HttpStatusCode.BadRequest, "BAD_REQUEST")
    {
    }
}