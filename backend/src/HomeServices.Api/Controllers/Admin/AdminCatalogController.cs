using HomeServices.Api.Authorization;
using HomeServices.Application.Catalog;
using HomeServices.Application.Estimates;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using HomeServices.Domain.Staff;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Api.Controllers.Admin;

/// <summary>
/// Back Office management of service categories, work items, cities and districts (needs <c>catalog.manage</c>).
/// Names are sent per language: <c>{ "hy": "…", "ru": "…", "en": "…" }</c>; the default language is required.
/// </summary>
[ApiController]
[Route("api/v1/admin/catalog")]
[HasPermission(Permissions.CatalogManage)]
public sealed class AdminCatalogController : ControllerBase
{
    public sealed record CategoryRequest(string Slug, Dictionary<string, string> Name, string? Icon, Guid? ParentId, int SortOrder);

    /// <summary>A work item's fields. Prices (labour, AMD per unit) are all three or none.</summary>
    public sealed record WorkItemRequest(
        Guid CategoryId,
        string Slug,
        Dictionary<string, string> Name,
        WorkUnit Unit,
        WorkSurface Surface,
        int SortOrder,
        int? PriceMin = null,
        int? PriceTypical = null,
        int? PriceMax = null,
        bool IsPriceLocked = false);

    public sealed record PlaceRequest(string Slug, Dictionary<string, string> Name, int SortOrder);

    /// <summary>A town or village: its fields plus the region (null for none) and whether it is a town or a village.</summary>
    public sealed record CityRequest(string Slug, Dictionary<string, string> Name, int SortOrder, Guid? RegionId = null, SettlementKind Kind = SettlementKind.City);

    // ---------- Categories ----------

    /// <summary>All categories as a tree, inactive ones included.</summary>
    [HttpGet("categories")]
    public Task<IReadOnlyList<AdminCategoryNodeDto>> GetCategories(
        [FromServices] IQueryHandler<GetAdminCategories, IReadOnlyList<AdminCategoryNodeDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminCategories(), cancellationToken);

    [HttpPost("categories")]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategoryAsync(
        CategoryRequest request,
        [FromServices] ICommandHandler<CreateCategory, AdminCategoryDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new CreateCategory(request.Slug, request.Name, request.Icon, request.ParentId, request.SortOrder),
                cancellationToken));

    [HttpPut("categories/{id:guid}")]
    public Task<AdminCategoryDto> UpdateCategory(
        Guid id,
        CategoryRequest request,
        [FromServices] ICommandHandler<UpdateCategory, AdminCategoryDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new UpdateCategory(id, request.Slug, request.Name, request.Icon, request.ParentId, request.SortOrder),
            cancellationToken);

    /// <summary>Shows the category on the Portal again.</summary>
    [HttpPost("categories/{id:guid}/activate")]
    public Task<AdminCategoryDto> ActivateCategory(
        Guid id,
        [FromServices] ICommandHandler<SetCategoryActive, AdminCategoryDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCategoryActive(id, IsActive: true), cancellationToken);

    /// <summary>Hides the category (and its subcategories) on the Portal.</summary>
    [HttpPost("categories/{id:guid}/deactivate")]
    public Task<AdminCategoryDto> DeactivateCategory(
        Guid id,
        [FromServices] ICommandHandler<SetCategoryActive, AdminCategoryDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCategoryActive(id, IsActive: false), cancellationToken);

    /// <summary>Deletes a category without subcategories.</summary>
    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategoryAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteCategory, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteCategory(id), cancellationToken);
        return NoContent();
    }

    // ---------- Work items ----------

    /// <summary>
    /// Work items in catalog order, hidden ones included. <paramref name="categoryId"/> may be a main category (its
    /// subcategories' items); <paramref name="search"/> matches the slug or any name.
    /// </summary>
    [HttpGet("work-items")]
    public Task<IReadOnlyList<AdminWorkItemDto>> GetWorkItems(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromServices] IQueryHandler<GetAdminWorkItems, IReadOnlyList<AdminWorkItemDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminWorkItems(categoryId, search, isActive), cancellationToken);

    /// <summary>Adds a work item under a subcategory.</summary>
    [HttpPost("work-items")]
    public async Task<ActionResult<AdminWorkItemDto>> CreateWorkItemAsync(
        WorkItemRequest request,
        [FromServices] ICommandHandler<CreateWorkItem, AdminWorkItemDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(
                new CreateWorkItem(
                    request.CategoryId, request.Slug, request.Name, request.Unit, request.Surface, request.SortOrder,
                    request.PriceMin, request.PriceTypical, request.PriceMax, request.IsPriceLocked),
                cancellationToken));

    [HttpPut("work-items/{id:guid}")]
    public Task<AdminWorkItemDto> UpdateWorkItem(
        Guid id,
        WorkItemRequest request,
        [FromServices] ICommandHandler<UpdateWorkItem, AdminWorkItemDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(
            new UpdateWorkItem(
                id, request.CategoryId, request.Slug, request.Name, request.Unit, request.Surface, request.SortOrder,
                request.PriceMin, request.PriceTypical, request.PriceMax, request.IsPriceLocked),
            cancellationToken);

    /// <summary>Shows the work item on the Portal again.</summary>
    [HttpPost("work-items/{id:guid}/activate")]
    public Task<AdminWorkItemDto> ActivateWorkItem(
        Guid id,
        [FromServices] ICommandHandler<SetWorkItemActive, AdminWorkItemDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetWorkItemActive(id, IsActive: true), cancellationToken);

    /// <summary>Hides the work item on the Portal.</summary>
    [HttpPost("work-items/{id:guid}/deactivate")]
    public Task<AdminWorkItemDto> DeactivateWorkItem(
        Guid id,
        [FromServices] ICommandHandler<SetWorkItemActive, AdminWorkItemDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetWorkItemActive(id, IsActive: false), cancellationToken);

    [HttpDelete("work-items/{id:guid}")]
    public async Task<IActionResult> DeleteWorkItemAsync(
        Guid id,
        [FromServices] ICommandHandler<DeleteWorkItem, bool> handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteWorkItem(id), cancellationToken);
        return NoContent();
    }

    // ---------- Room templates (the estimator's usual work per kind of room) ----------

    public sealed record RoomTemplateRequest(IReadOnlyList<RoomTemplateInput> Items);

    /// <summary>Every room type's usual work, in order, with default counts.</summary>
    [HttpGet("room-templates")]
    public Task<IReadOnlyList<AdminRoomTemplateDto>> GetRoomTemplates(
        [FromServices] IQueryHandler<GetAdminRoomTemplates, IReadOnlyList<AdminRoomTemplateDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminRoomTemplates(), cancellationToken);

    /// <summary>Replaces a room type's usual work with the list sent (in that order).</summary>
    [HttpPut("room-templates/{roomType}")]
    public Task<AdminRoomTemplateDto> SetRoomTemplate(
        RoomType roomType,
        RoomTemplateRequest request,
        [FromServices] ICommandHandler<SetRoomTemplate, AdminRoomTemplateDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetRoomTemplate(roomType, request.Items), cancellationToken);

    // ---------- Regions, cities and districts ----------

    /// <summary>Yerevan and the regions.</summary>
    [HttpGet("regions")]
    public Task<IReadOnlyList<AdminRegionDto>> GetRegions(
        [FromServices] IQueryHandler<GetAdminRegions, IReadOnlyList<AdminRegionDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminRegions(), cancellationToken);

    /// <summary>All cities with their districts, inactive ones included.</summary>
    [HttpGet("cities")]
    public Task<IReadOnlyList<AdminCityDto>> GetCities(
        [FromServices] IQueryHandler<GetAdminCities, IReadOnlyList<AdminCityDto>> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new GetAdminCities(), cancellationToken);

    [HttpPost("cities")]
    public async Task<ActionResult<AdminCityDto>> CreateCityAsync(
        CityRequest request,
        [FromServices] ICommandHandler<CreateCity, AdminCityDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new CreateCity(request.Slug, request.Name, request.SortOrder, request.RegionId, request.Kind), cancellationToken));

    [HttpPut("cities/{id:guid}")]
    public Task<AdminCityDto> UpdateCity(
        Guid id,
        CityRequest request,
        [FromServices] ICommandHandler<UpdateCity, AdminCityDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateCity(id, request.Slug, request.Name, request.SortOrder, request.RegionId, request.Kind), cancellationToken);

    [HttpPost("cities/{id:guid}/activate")]
    public Task<AdminCityDto> ActivateCity(
        Guid id,
        [FromServices] ICommandHandler<SetCityActive, AdminCityDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCityActive(id, IsActive: true), cancellationToken);

    [HttpPost("cities/{id:guid}/deactivate")]
    public Task<AdminCityDto> DeactivateCity(
        Guid id,
        [FromServices] ICommandHandler<SetCityActive, AdminCityDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetCityActive(id, IsActive: false), cancellationToken);

    [HttpPost("cities/{cityId:guid}/districts")]
    public async Task<ActionResult<AdminDistrictDto>> AddDistrictAsync(
        Guid cityId,
        PlaceRequest request,
        [FromServices] ICommandHandler<AddDistrict, AdminDistrictDto> handler,
        CancellationToken cancellationToken) =>
        StatusCode(
            StatusCodes.Status201Created,
            await handler.HandleAsync(new AddDistrict(cityId, request.Slug, request.Name, request.SortOrder), cancellationToken));

    [HttpPut("cities/{cityId:guid}/districts/{districtId:guid}")]
    public Task<AdminDistrictDto> UpdateDistrict(
        Guid cityId,
        Guid districtId,
        PlaceRequest request,
        [FromServices] ICommandHandler<UpdateDistrict, AdminDistrictDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new UpdateDistrict(cityId, districtId, request.Slug, request.Name, request.SortOrder), cancellationToken);

    [HttpPost("cities/{cityId:guid}/districts/{districtId:guid}/activate")]
    public Task<AdminDistrictDto> ActivateDistrict(
        Guid cityId,
        Guid districtId,
        [FromServices] ICommandHandler<SetDistrictActive, AdminDistrictDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetDistrictActive(cityId, districtId, IsActive: true), cancellationToken);

    [HttpPost("cities/{cityId:guid}/districts/{districtId:guid}/deactivate")]
    public Task<AdminDistrictDto> DeactivateDistrict(
        Guid cityId,
        Guid districtId,
        [FromServices] ICommandHandler<SetDistrictActive, AdminDistrictDto> handler,
        CancellationToken cancellationToken) =>
        handler.HandleAsync(new SetDistrictActive(cityId, districtId, IsActive: false), cancellationToken);
}
