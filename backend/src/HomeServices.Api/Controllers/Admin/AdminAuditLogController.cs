using HomeServices.Api.Authorization;
using HomeServices.Application.Auditing;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Auditing;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Who changed what and when, across the Back Office and the Portal.</summary>
[ApiController]
[Route("api/v1/admin/audit-log")]
public sealed class AdminAuditLogController : ControllerBase
{
    /// <summary>Audit entries, newest first. All filters are optional; <paramref name="pageSize"/> is at most 200.</summary>
    [HttpGet]
    [HasPermission(Permissions.AuditView)]
    public Task<PagedResult<AuditLogEntryDto>> Get(
        [FromServices] IQueryHandler<GetAuditLog, PagedResult<AuditLogEntryDto>> handler,
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] string? actorId,
        [FromQuery] AuditAction? action,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetAuditLog(entityType, entityId, actorId, action, from, to, page ?? 1, pageSize ?? GetAuditLog.DefaultPageSize),
            cancellationToken);
}
