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

public sealed class ConflictException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Conflict";
}
