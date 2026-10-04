using HomeServices.Application.Messaging;
using HomeServices.Application.Staff;
using HomeServices.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>The signed-in staff member's own account.</summary>
[ApiController]
[Authorize(Policy = AuthPolicies.Staff)]
[Route("api/v1/admin/me")]
public sealed class AdminMeController : ControllerBase
{
    [HttpGet]
    public Task<StaffProfileDto> Get(
        [FromServices] IQueryHandler<GetStaffProfile, StaffProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetStaffProfile(), cancellationToken);
}
