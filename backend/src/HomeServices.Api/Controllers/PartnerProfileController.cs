using HomeServices.Application.Messaging;
using HomeServices.Application.Partners;
using HomeServices.Domain.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers;

/// <summary>
/// The signed-in user's partner profile: create and edit it, attach work examples and documents
/// (upload them with /api/v1/files first), then submit it for review.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/me/partner-profile")]
public sealed class PartnerProfileController : ControllerBase
{
    public sealed record SaveRequest(
        PartnerType Type,
        string DisplayName,
        string? About,
        int? YearsOfExperience,
        Guid? AvatarFileId,
        IReadOnlyList<Guid> CategoryIds,
        IReadOnlyList<PartnerAreaDto> Areas);

    public sealed record AddMediaRequest(PartnerMediaKind Kind, Guid FileId, string? Caption);

    /// <summary>The partner's price list: the full list of prices for the services they offer.</summary>
    public sealed record SavePricesRequest(IReadOnlyList<MyPriceInput> Prices);

    /// <summary>The profile, or 404 "partner.not_found" when the user hasn't started one.</summary>
    [HttpGet]
    public Task<PartnerProfileDto> Get(
        [FromServices] IQueryHandler<GetMyPartnerProfile, PartnerProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyPartnerProfile(), cancellationToken);

    /// <summary>Creates or updates the profile. Services and areas are replaced by the lists sent.</summary>
    [HttpPut]
    public Task<PartnerProfileDto> Save(
        SaveRequest request,
        [FromServices] ICommandHandler<SaveMyPartnerProfile, PartnerProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new SaveMyPartnerProfile(
                request.Type,
                request.DisplayName,
                request.About,
                request.YearsOfExperience,
                request.AvatarFileId,
                request.CategoryIds,
                request.Areas),
            cancellationToken);

    /// <summary>Attaches an uploaded file as a work example or document.</summary>
    [HttpPost("media")]
    public Task<PartnerProfileDto> AddMedia(
        AddMediaRequest request,
        [FromServices] ICommandHandler<AddMyPartnerMedia, PartnerProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new AddMyPartnerMedia(request.Kind, request.FileId, request.Caption), cancellationToken);

    [HttpDelete("media/{mediaId:guid}")]
    public Task<PartnerProfileDto> RemoveMedia(
        Guid mediaId,
        [FromServices] ICommandHandler<RemoveMyPartnerMedia, PartnerProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new RemoveMyPartnerMedia(mediaId), cancellationToken);

    /// <summary>Sends the profile for review. 422 "partner.incomplete" lists nothing; read <c>missingForSubmit</c> from GET first.</summary>
    [HttpPost("submit")]
    public Task<PartnerProfileDto> Submit(
        [FromServices] ICommandHandler<SubmitMyPartnerProfile, PartnerProfileDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SubmitMyPartnerProfile(), cancellationToken);

    /// <summary>
    /// The price list: every work item in the services the partner offers, with the market range and their own price.
    /// </summary>
    [HttpGet("prices")]
    public Task<MyPriceListDto> GetPrices(
        [FromServices] IQueryHandler<GetMyPrices, MyPriceListDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetMyPrices(), cancellationToken);

    /// <summary>
    /// Saves the price list. Send every price: items of the offered services left out lose their price.
    /// 422 "partner_price.not_offered" for an item outside the partner's services.
    /// </summary>
    [HttpPut("prices")]
    public Task<MyPriceListDto> SavePrices(
        SavePricesRequest request,
        [FromServices] ICommandHandler<SaveMyPrices, MyPriceListDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SaveMyPrices(request.Prices), cancellationToken);
}
