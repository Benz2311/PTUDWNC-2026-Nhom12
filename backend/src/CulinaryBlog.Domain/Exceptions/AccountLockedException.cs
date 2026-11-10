using System.Net;

namespace CulinaryBlog.Domain.Exceptions;

public sealed class AccountLockedException(DateTime lockoutEnd, DateTime now)
    : BaseDomainException(
        $"Account is temporarily locked. Retry in {Math.Ceiling((lockoutEnd - now).TotalSeconds)} seconds.",
        HttpStatusCode.Locked, "AUTH_ACCOUNT_LOCKED");
