using FluentValidation;
using HomeServices.Application.Errors;
using HomeServices.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Api.Errors;

/// <summary>
/// Turns every exception into RFC 7807 ProblemDetails with a stable <c>code</c>.
/// Unexpected errors are logged and returned as a generic 500 without internal details.
/// </summary>
public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => ValidationProblem(validation),
            NotFoundException e => Problem(StatusCodes.Status404NotFound, "Not found", e.Code, e.Message),
            ConflictException e => Problem(StatusCodes.Status409Conflict, "Conflict", e.Code, e.Message),
            UnauthorizedException e => Problem(StatusCodes.Status401Unauthorized, "Unauthorized", e.Code, e.Message),
            ForbiddenException e => Problem(StatusCodes.Status403Forbidden, "Forbidden", e.Code, e.Message),
            TooManyRequestsException e => Problem(StatusCodes.Status429TooManyRequests, "Too many requests", e.Code, e.Message),
            DomainException e => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violated", e.Code, e.Message),
            DbUpdateConcurrencyException => Problem(
                StatusCodes.Status409Conflict, "Conflict", "concurrency_conflict", "Someone else changed this at the same time. Reload and try again."),
            _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred", "internal_error", detail: null),
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(logger, exception);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string code, string? detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ProblemDetails ValidationProblem(ValidationException exception)
    {
        var problem = Problem(StatusCodes.Status400BadRequest, "Validation failed", "validation_failed", detail: null);
        problem.Extensions["errors"] = exception.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorCode).ToArray());
        return problem;
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandled(ILogger logger, Exception exception);
}
