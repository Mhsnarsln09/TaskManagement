namespace TaskManagement.Application.Errors;

public abstract class ApplicationExceptionBase(string message) : Exception(message)
{
    public abstract int StatusCode { get; }

    public virtual string Title => GetType().Name.Replace("Exception", string.Empty);
}

public sealed class NotFoundException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 404;

    public override string Title => "Resource not found";
}

public sealed class ForbiddenException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 403;

    public override string Title => "Forbidden";
}

public sealed class UnauthorizedException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 401;

    public override string Title => "Unauthorized";
}

public sealed class ConflictException(string message) : ApplicationExceptionBase(message)
{
    public override int StatusCode => 409;

    public override string Title => "Conflict";
}

public sealed class ValidationProblemException(IReadOnlyDictionary<string, string[]> errors)
    : ApplicationExceptionBase("One or more validation errors occurred.")
{
    public override int StatusCode => 400;

    public override string Title => "Validation failed";

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
