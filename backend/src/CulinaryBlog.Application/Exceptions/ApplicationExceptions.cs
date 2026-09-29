namespace CulinaryBlog.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
    }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to access this resource.")
        : base(message)
    {
    }
}

public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.")
    {
        Errors = errors;
    }
}
