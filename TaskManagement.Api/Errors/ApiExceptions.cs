namespace TaskManagement.Api.Errors;

public abstract class ApiException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }

    public virtual string Title => GetType().Name.Replace("Exception", string.Empty);
}

public sealed class NotFoundException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Resource not found";
}

public sealed class ForbiddenException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;

    public override string Title => "Forbidden";
}

public sealed class UnauthorizedException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;

    public override string Title => "Unauthorized";
}

public sealed class ConflictException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Conflict";
}

public sealed class ValidationProblemException(IReadOnlyDictionary<string, string[]> errors)
    : ApiException("One or more validation errors occurred.")
{
    public override int StatusCode => StatusCodes.Status400BadRequest;

    public override string Title => "Validation failed";

    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
