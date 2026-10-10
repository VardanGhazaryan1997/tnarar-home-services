using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Estimates;

/// <summary>The work usually wanted in each kind of room (active work only), names in the request language.</summary>
public sealed record GetRoomTemplates : IQuery<IReadOnlyList<RoomTemplateDto>>;

public sealed record RoomTemplateDto(RoomType Type, IReadOnlyList<RoomTemplateItemDto> Items);

/// <summary>
/// A work item of a template: its market range, and the default count for work that can't be measured from the room
/// (<see cref="Quantity"/>, or <see cref="QuantityPerSquareMeter"/> of floor); both null = measured.
/// </summary>
public sealed record RoomTemplateItemDto(
    Guid WorkItemId,
    string Slug,
    string Name,
    WorkUnit Unit,
    WorkSurface Surface,
    decimal? Quantity,
    decimal? QuantityPerSquareMeter,
    int? MarketMin,
    int? MarketTypical,
    int? MarketMax);

/// <summary>
/// A quick estimate: one room of <paramref name="Area"/> m² with the usual work for its type (see room templates),
/// measured and priced. For the "how much will it cost?" widget.
/// </summary>
public sealed record QuickEstimate(RoomType RoomType, decimal Area, decimal? Height = null, bool OldBuilding = false)
    : IQuery<EstimateMeasurementDto>;

/// <summary>Every room type's template with all translations and hidden work too, for the Back Office.</summary>
public sealed record GetAdminRoomTemplates : IQuery<IReadOnlyList<AdminRoomTemplateDto>>;

public sealed record AdminRoomTemplateDto(RoomType Type, IReadOnlyList<RoomTemplateInput> Items);

/// <summary>A template line as staff edit it.</summary>
public sealed record RoomTemplateInput(Guid WorkItemId, decimal? Quantity = null, decimal? QuantityPerSquareMeter = null);

/// <summary>Replaces the work of a room type's template with <paramref name="Items"/> (in this order).</summary>
public sealed record SetRoomTemplate(RoomType RoomType, IReadOnlyList<RoomTemplateInput> Items) : ICommand<AdminRoomTemplateDto>;

public sealed class QuickEstimateValidator : AbstractValidator<QuickEstimate>
{
    public QuickEstimateValidator()
    {
        RuleFor(x => x.RoomType).IsInEnum().WithErrorCode("room.type_invalid");
        RuleFor(x => x.Area).Must(area => area is > 0 and <= RoomSize.MaxArea).WithErrorCode("area.invalid");
        RuleFor(x => x.Height).InclusiveBetween(RoomSize.MinHeight, RoomSize.MaxHeight).When(x => x.Height is not null).WithErrorCode("height.invalid");
    }
}

public sealed class SetRoomTemplateValidator : AbstractValidator<SetRoomTemplate>
{
    public SetRoomTemplateValidator()
    {
        RuleFor(x => x.RoomType).IsInEnum().WithErrorCode("room.type_invalid");
        RuleFor(x => x.Items).NotNull().WithErrorCode("items.required")
            .Must(items => items is null || items.Count <= EstimateRoom.MaxLines).WithErrorCode("items.too_many")
            .Must(items => items is null || items.Select(i => i.WorkItemId).Distinct().Count() == items.Count).WithErrorCode("items.duplicate");
    }
}

public sealed class GetRoomTemplatesHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetRoomTemplates, IReadOnlyList<RoomTemplateDto>>
{
    public async Task<IReadOnlyList<RoomTemplateDto>> HandleAsync(GetRoomTemplates query, CancellationToken cancellationToken)
    {
        var templates = await RoomTemplateData.ActiveAsync(db, roomType: null, cancellationToken);
        return Enum.GetValues<RoomType>()
            .Select(type => new RoomTemplateDto(
                type,
                templates.Where(t => t.Template.RoomType == type)
                    .Select(t => new RoomTemplateItemDto(
                        t.Item.Id,
                        t.Item.Slug,
                        t.Item.Name.Get(language.Code, language.DefaultCode),
                        t.Item.Unit,
                        t.Item.Surface,
                        t.Template.Quantity,
                        t.Template.QuantityPerSquareMeter,
                        t.Item.MarketMin,
                        t.Item.MarketTypical,
                        t.Item.MarketMax))
                    .ToList()))
            .ToList();
    }
}

public sealed class QuickEstimateHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<QuickEstimate, EstimateMeasurementDto>
{
    public async Task<EstimateMeasurementDto> HandleAsync(QuickEstimate query, CancellationToken cancellationToken)
    {
        var templates = await RoomTemplateData.ActiveAsync(db, query.RoomType, cancellationToken);
        var lines = templates.Select(t => new LineInput(t.Item.Id, t.Template.DefaultQuantity(query.Area))).ToList();
        var room = new RoomInput(query.RoomType, null, null, query.Area, query.Height, Lines: lines);
        return await EstimateCalculator.CalculateAsync(db, language, [room], query.OldBuilding, skipUnavailable: false, cancellationToken);
    }
}

public sealed class GetAdminRoomTemplatesHandler(IAppDbContext db) : IQueryHandler<GetAdminRoomTemplates, IReadOnlyList<AdminRoomTemplateDto>>
{
    public async Task<IReadOnlyList<AdminRoomTemplateDto>> HandleAsync(GetAdminRoomTemplates query, CancellationToken cancellationToken)
    {
        var templates = await db.RoomTemplates.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync(cancellationToken);
        return Enum.GetValues<RoomType>().Select(type => RoomTemplateData.ToAdmin(type, templates)).ToList();
    }
}

public sealed class SetRoomTemplateHandler(IAppDbContext db) : ICommandHandler<SetRoomTemplate, AdminRoomTemplateDto>
{
    public async Task<AdminRoomTemplateDto> HandleAsync(SetRoomTemplate command, CancellationToken cancellationToken)
    {
        var ids = command.Items.Select(i => i.WorkItemId).ToList();
        var items = await db.WorkItems.AsNoTracking().Where(w => ids.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);
        if (items.Count != ids.Count)
        {
            throw new DomainException("room_template.work_item_not_found", "Some of the work no longer exists.");
        }

        if (command.Items.Any(i => i.Quantity is null && i.QuantityPerSquareMeter is null && !RoomGeometry.CanMeasure(items[i.WorkItemId].Unit, items[i.WorkItemId].Surface)))
        {
            throw new DomainException("room_template.quantity_required", "Work that can't be measured from the room needs a count.");
        }

        var created = command.Items
            .Select((item, index) => RoomTemplate.Create(command.RoomType, item.WorkItemId, index + 1, item.Quantity, item.QuantityPerSquareMeter))
            .ToList();
        db.RoomTemplates.RemoveRange(await db.RoomTemplates.Where(t => t.RoomType == command.RoomType).ToListAsync(cancellationToken));
        db.RoomTemplates.AddRange(created);
        await db.SaveChangesAsync(cancellationToken);
        return RoomTemplateData.ToAdmin(command.RoomType, created);
    }
}

internal static class RoomTemplateData
{
    public sealed record ActiveTemplate(RoomTemplate Template, WorkItem Item);

    /// <summary>Template lines whose work is active, in order; for one room type or all.</summary>
    public static async Task<List<ActiveTemplate>> ActiveAsync(IAppDbContext db, RoomType? roomType, CancellationToken cancellationToken)
    {
        var templates = await db.RoomTemplates.AsNoTracking()
            .Where(t => roomType == null || t.RoomType == roomType)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);
        var ids = templates.Select(t => t.WorkItemId).Distinct().ToList();
        var items = await db.WorkItems.AsNoTracking().Where(w => ids.Contains(w.Id) && w.IsActive).ToDictionaryAsync(w => w.Id, cancellationToken);
        return templates.Where(t => items.ContainsKey(t.WorkItemId)).Select(t => new ActiveTemplate(t, items[t.WorkItemId])).ToList();
    }

    public static AdminRoomTemplateDto ToAdmin(RoomType type, IEnumerable<RoomTemplate> templates) =>
        new(type, templates.Where(t => t.RoomType == type)
            .OrderBy(t => t.SortOrder)
            .Select(t => new RoomTemplateInput(t.WorkItemId, t.Quantity, t.QuantityPerSquareMeter))
            .ToList());
}
