using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class SecurityBreachException : BaseDomainException
{
    public SecurityBreachException(string message = "Security breach detected.")
        : base(message, HttpStatusCode.Forbidden, "SECURITY_BREACH_DETECTED")
    {
    }
}