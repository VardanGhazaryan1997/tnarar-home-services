using FluentValidation;
using FluentValidation.Results;
using HomeServices.Application.Errors;
using HomeServices.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.IntegrationTests.Infrastructure;

/// <summary>Test-only endpoints that throw each kind of error.</summary>
[ApiController]
[Route("api/v1/test/errors")]
public sealed class ErrorsTestController : ControllerBase
{
    public const string SecretMessage = "connection string with password=secret";

    public enum Colour
    {
        Red = 1,
        Blue = 2,
    }

    public sealed record Body(string Name, Colour Colour, int Count);

    /// <summary>Echoes a body, to test how unreadable requests are reported.</summary>
    [HttpPost("body")]
    public Body Echo(Body body) => body;

    [HttpGet("validation")]
    public IActionResult ThrowValidation() => throw new ValidationException(
    [
        new ValidationFailure("Name", "Name is required.") { ErrorCode = "name.required" },
        new ValidationFailure("Name", "Name is too short.") { ErrorCode = "name.too_short" },
        new ValidationFailure("PhoneNumber", "Phone is invalid.") { ErrorCode = "phone.invalid" },
    ]);

    [HttpGet("not-found")]
    public IActionResult ThrowNotFound() => throw new NotFoundException("Partner not found.", "partner.not_found");

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new ConflictException("Phone already registered.", "phone.taken");

    [HttpGet("forbidden")]
    public IActionResult ThrowForbidden() => throw new ForbiddenException("Not your order.");

    [HttpGet("unauthorized")]
    public IActionResult ThrowUnauthorized() => throw new UnauthorizedException("Please sign in.", "session.expired");

    [HttpGet("too-many")]
    public IActionResult ThrowTooMany() => throw new TooManyRequestsException("Slow down.", "otp.resend_too_soon");

    [HttpGet("domain")]
    public IActionResult ThrowDomain() => throw new DomainException("order.not_in_progress", "The order has not started yet.");

    [HttpGet("unhandled")]
    public IActionResult ThrowUnhandled() => throw new InvalidOperationException(SecretMessage);
}
