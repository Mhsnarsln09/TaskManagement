using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
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
}
