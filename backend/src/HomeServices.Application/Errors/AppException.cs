namespace HomeServices.Application.Errors;

/// <summary>
/// Base for expected application errors. <see cref="Code"/> is stable and translated
/// by the frontend; the API maps each subclass to an HTTP status.
/// </summary>
public abstract class AppException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>HTTP 404.</summary>
public sealed class NotFoundException(string message, string code = "not_found") : AppException(message, code);

/// <summary>HTTP 409 — e.g. a duplicate, or the record changed since it was read.</summary>
public sealed class ConflictException(string message, string code = "conflict") : AppException(message, code);

/// <summary>HTTP 403 — authenticated, but not allowed to do this.</summary>
public sealed class ForbiddenException(string message, string code = "forbidden") : AppException(message, code);

/// <summary>HTTP 401 — not signed in, or the session is no longer valid.</summary>
public sealed class UnauthorizedException(string message, string code = "unauthorized") : AppException(message, code);

/// <summary>HTTP 429 — slow down (e.g. requesting SMS codes too often).</summary>
public sealed class TooManyRequestsException(string message, string code = "too_many_requests") : AppException(message, code);
