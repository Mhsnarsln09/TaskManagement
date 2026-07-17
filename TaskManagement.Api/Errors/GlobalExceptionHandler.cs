using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Common;

namespace TaskManagement.Api.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode = exception switch
        {
            ApplicationExceptionBase handledApplicationException => handledApplicationException.StatusCode,
            DomainException => StatusCodes.Status409Conflict,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            // Race conditions can slip past application-level duplicate checks; the
            // database unique constraint is the backstop and must surface as 409.
            DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException) =>
                StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
        }

        httpContext.Response.StatusCode = statusCode;

        ProblemDetails problem = exception is ValidationProblemException validationProblemException
            ? new HttpValidationProblemDetails(validationProblemException.Errors)
            : new ProblemDetails();

        problem.Status = statusCode;
        problem.Title = exception is ApplicationExceptionBase applicationException
            ? applicationException.Title
            : ReasonPhrases.GetReasonPhrase(statusCode);
        problem.Detail = exception switch
        {
            DbUpdateConcurrencyException =>
                "The resource was modified by another request. Refresh and retry.",
            // Unexpected exceptions must not leak internal details to clients.
            _ when statusCode == StatusCodes.Status500InternalServerError =>
                "An unexpected error occurred.",
            DbUpdateException =>
                "The change conflicts with data that already exists.",
            _ => exception.Message
        };
        problem.Instance = httpContext.Request.Path;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            PostgresException postgresException =>
                postgresException.SqlState == PostgresErrorCodes.UniqueViolation,
            // The Sqlite provider (integration tests) is not referenced by the Api
            // project, so its unique violation is detected by message.
            { } innerException =>
                innerException.Message.Contains("UNIQUE constraint failed", StringComparison.Ordinal),
            null => false
        };
    }
}
