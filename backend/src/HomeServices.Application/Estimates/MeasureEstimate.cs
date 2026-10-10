using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Estimates;

/// <summary>
/// Measures rooms without saving anything: floor, wall and ceiling areas, perimeter and skirting length of each room,
/// and the quantity of each work item wanted in it (measured from the room unless the customer gave one). Anyone can
/// call it; the estimator uses it while the customer fills in rooms.
/// </summary>
public sealed record MeasureEstimate(IReadOnlyList<RoomInput> Rooms) : IQuery<EstimateMeasurementDto>;

/// <summary>A room: length × width, or only the floor area; height defaults to 2.7 m.</summary>
public sealed record RoomInput(
    RoomType Type,
    decimal? Length,
    decimal? Width,
    decimal? Area,
    decimal? Height,
    IReadOnlyList<OpeningInput>? Openings = null,
    IReadOnlyList<LineInput>? Lines = null);

public sealed record OpeningInput(OpeningKind Kind, decimal Width, decimal Height, int Count = 1);

/// <summary>Work wanted in the room; <see cref="Quantity"/> overrides the measured quantity.</summary>
public sealed record LineInput(Guid WorkItemId, decimal? Quantity = null);

public sealed record EstimateMeasurementDto(IReadOnlyList<RoomMeasurementDto> Rooms);

public sealed record RoomMeasurementDto(
    RoomType Type,
    decimal FloorArea,
    decimal Perimeter,
    decimal WallArea,
    decimal CeilingArea,
    decimal SkirtingLength,
    IReadOnlyList<LineMeasurementDto> Lines);

/// <summary>
/// A line: <see cref="MeasuredQuantity"/> from the room (null when the work can't be measured, e.g. pieces),
/// <see cref="Quantity"/> the one used (the customer's, else the measured one); <see cref="NeedsQuantity"/> when neither.
/// </summary>
public sealed record LineMeasurementDto(
    Guid WorkItemId,
    string Name,
    WorkUnit Unit,
    WorkSurface Surface,
    decimal? MeasuredQuantity,
    decimal? Quantity,
    bool NeedsQuantity);

public sealed class MeasureEstimateValidator : AbstractValidator<MeasureEstimate>
{
    public MeasureEstimateValidator()
    {
        RuleFor(x => x.Rooms).NotNull().WithErrorCode("rooms.required")
            .Must(rooms => rooms is null || rooms.Count is >= 1 and <= Estimate.MaxRooms).WithErrorCode("rooms.count_invalid");
        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.Type).IsInEnum().WithErrorCode("room.type_invalid");
            room.RuleFor(r => r.Openings)
                .Must(openings => openings is null || openings.Count <= EstimateRoom.MaxOpenings)
                .WithErrorCode("room.too_many_openings");
            room.RuleFor(r => r.Lines)
                .Must(lines => lines is null || lines.Count <= EstimateRoom.MaxLines)
                .WithErrorCode("room.too_many_lines");
            room.RuleFor(r => r.Lines)
                .Must(lines => lines is null || lines.Select(l => l.WorkItemId).Distinct().Count() == lines.Count)
                .WithErrorCode("room.duplicate_lines");
            room.RuleForEach(r => r.Lines)
                .Must(line => line.Quantity is null or (> 0 and <= EstimateLine.MaxQuantity))
                .WithErrorCode("line.quantity_invalid");
        });
    }
}

public sealed class MeasureEstimateHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<MeasureEstimate, EstimateMeasurementDto>
{
    public async Task<EstimateMeasurementDto> HandleAsync(MeasureEstimate query, CancellationToken cancellationToken)
    {
        var ids = query.Rooms.SelectMany(r => r.Lines ?? Array.Empty<LineInput>()).Select(l => l.WorkItemId).Distinct().ToList();
        var items = await db.WorkItems.AsNoTracking()
            .Where(w => ids.Contains(w.Id) && w.IsActive)
            .ToDictionaryAsync(w => w.Id, cancellationToken);
        if (items.Count != ids.Count)
        {
            throw new DomainException("estimate.work_item_not_found", "Some of the work is no longer offered. Refresh the page.");
        }

        return new EstimateMeasurementDto(query.Rooms.Select(room => Measure(room, items)).ToList());
    }

    private RoomMeasurementDto Measure(RoomInput room, Dictionary<Guid, WorkItem> items)
    {
        var geometry = RoomGeometry.Of(
            new RoomSize(room.Length, room.Width, room.Area, room.Height ?? RoomSize.DefaultHeight),
            (room.Openings ?? Array.Empty<OpeningInput>()).Select(o => new RoomOpening(o.Kind, o.Width, o.Height, o.Count)));

        var lines = (room.Lines ?? Array.Empty<LineInput>())
            .Select(line =>
            {
                var item = items[line.WorkItemId];
                var measured = geometry.QuantityFor(item.Unit, item.Surface);
                var quantity = line.Quantity ?? measured;
                return new LineMeasurementDto(
                    item.Id, item.Name.Get(language.Code, language.DefaultCode), item.Unit, item.Surface, measured, quantity, quantity is null);
            })
            .ToList();

        return new RoomMeasurementDto(
            room.Type, geometry.FloorArea, geometry.Perimeter, geometry.WallArea, geometry.CeilingArea, geometry.SkirtingLength, lines);
    }
}
