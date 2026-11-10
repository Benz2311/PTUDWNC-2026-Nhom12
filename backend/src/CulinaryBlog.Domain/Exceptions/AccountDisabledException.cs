using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class AccountDisabledException()
    : BaseDomainException("Account is inactive.", HttpStatusCode.Forbidden, "AUTH_ACCOUNT_DISABLED");
