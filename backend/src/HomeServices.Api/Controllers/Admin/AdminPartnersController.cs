using HomeServices.Api.Authorization;
using HomeServices.Application.Common;
using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Partners;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>Partner review: the queue, a profile with its documents and history, and staff decisions.</summary>
[ApiController]
[Route("api/v1/admin/partners")]
public sealed class AdminPartnersController : ControllerBase
{
    public sealed record CommentRequest(string? Comment);

    /// <summary>
    /// Partner profiles. <c>?status=UnderReview</c> is the review queue (oldest first). <paramref name="search"/>
    /// matches the name or the owner's phone number. <paramref name="pageSize"/> is at most 100.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.PartnersView)]
    public Task<PagedResult<AdminPartnerListItemDto>> List(
        [FromServices] IQueryHandler<GetAdminPartners, PagedResult<AdminPartnerListItemDto>> handler,
        [FromQuery] PartnerStatus? status,
        [FromQuery] PartnerType? type,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new GetAdminPartners(status, type, search, page ?? 1, pageSize ?? GetAdminPartners.DefaultPageSize),
            cancellationToken);

    /// <summary>The profile (documents included), its owner and the review history, newest first.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.PartnersView)]
    public Task<AdminPartnerDto> Get(
        Guid id,
        [FromServices] IQueryHandler<GetAdminPartner, AdminPartnerDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminPartner(id), cancellationToken);

    /// <summary>Under review → Approved: the partner becomes public.</summary>
    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.PartnersApprove)]
    public Task<AdminPartnerDto> Approve(Guid id, [FromServices] ICommandHandler<DecideOnPartner, AdminPartnerDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DecideOnPartner(id, PartnerDecision.Approve), cancellationToken);

    /// <summary>Under review → Needs changes, with what to fix (<c>comment</c> required).</summary>
    [HttpPost("{id:guid}/request-changes")]
    [HasPermission(Permissions.PartnersApprove)]
    public Task<AdminPartnerDto> RequestChanges(Guid id, CommentRequest request, [FromServices] ICommandHandler<DecideOnPartner, AdminPartnerDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DecideOnPartner(id, PartnerDecision.RequestChanges, request.Comment), cancellationToken);

    /// <summary>Under review → Rejected (<c>comment</c> required). Final.</summary>
    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.PartnersApprove)]
    public Task<AdminPartnerDto> Reject(Guid id, CommentRequest request, [FromServices] ICommandHandler<DecideOnPartner, AdminPartnerDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DecideOnPartner(id, PartnerDecision.Reject, request.Comment), cancellationToken);

    /// <summary>Approved → Suspended: hidden from the Portal (<c>comment</c> required).</summary>
    [HttpPost("{id:guid}/suspend")]
    [HasPermission(Permissions.PartnersApprove)]
    public Task<AdminPartnerDto> Suspend(Guid id, CommentRequest request, [FromServices] ICommandHandler<DecideOnPartner, AdminPartnerDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DecideOnPartner(id, PartnerDecision.Suspend, request.Comment), cancellationToken);

    /// <summary>Suspended → Approved.</summary>
    [HttpPost("{id:guid}/reinstate")]
    [HasPermission(Permissions.PartnersApprove)]
    public Task<AdminPartnerDto> Reinstate(Guid id, [FromServices] ICommandHandler<DecideOnPartner, AdminPartnerDto> handler, CancellationToken cancellationToken) =>
        handler.HandleAsync(new DecideOnPartner(id, PartnerDecision.Reinstate), cancellationToken);
}
