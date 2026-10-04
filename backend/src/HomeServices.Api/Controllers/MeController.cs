using HomeServices.Application.Identity;
using HomeServices.Application.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>The signed-in Portal user's own account.</summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController : ControllerBase
{
    public sealed record UpdateProfileRequest(string FullName, string? Email);

    [HttpGet]
    public Task<MyProfileDto> Get(
        [FromServices] IQueryHandler<GetMyProfile, MyProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyProfile(), cancellationToken);

    [HttpPut]
    public Task<MyProfileDto> Update(
        UpdateProfileRequest request,
        [FromServices] ICommandHandler<UpdateMyProfile, MyProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateMyProfile(request.FullName, request.Email), cancellationToken);
}
