using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Catalog;
using HomeServices.Domain.Estimates;

namespace HomeServices.Application.Estimates;

/// <summary>
/// Measures and prices rooms without saving anything: floor, wall and ceiling areas, perimeter and skirting length of
/// each room; the quantity of each work item (measured from the room unless the customer gave one); and its labour price
/// range from the market range, with the adjustments (see <see cref="EstimatePricing"/>). Anyone can call it.
/// </summary>
public sealed record MeasureEstimate(IReadOnlyList<RoomInput> Rooms, bool OldBuilding = false) : IQuery<EstimateMeasurementDto>;

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

/// <summary>
/// The whole estimate. Totals add up the priced lines (labour, AMD); <see cref="UnpricedLines"/> counts lines left out
/// because they need a quantity or the work has no price yet.
/// </summary>
public sealed record EstimateMeasurementDto(
    IReadOnlyList<RoomMeasurementDto> Rooms,
    int TotalMin = 0,
    int TotalTypical = 0,
    int TotalMax = 0,
    int UnpricedLines = 0);

public sealed record RoomMeasurementDto(
    RoomType Type,
    decimal FloorArea,
    decimal Perimeter,
    decimal WallArea,
    decimal CeilingArea,
    decimal SkirtingLength,
    IReadOnlyList<LineMeasurementDto> Lines,
    int TotalMin = 0,
    int TotalTypical = 0,
    int TotalMax = 0);

/// <summary>
/// A line: <see cref="MeasuredQuantity"/> from the room (null when the work can't be measured, e.g. pieces),
/// <see cref="Quantity"/> the one used (the customer's, else the measured one); <see cref="NeedsQuantity"/> when neither.
/// Prices are the labour range for the quantity (null without a quantity or a market price); <see cref="Factor"/> is the
/// adjustment applied (1 = none).
/// </summary>
public sealed record LineMeasurementDto(
    Guid WorkItemId,
    string Name,
    WorkUnit Unit,
    WorkSurface Surface,
    decimal? MeasuredQuantity,
    decimal? Quantity,
    bool NeedsQuantity,
    int? PriceMin = null,
    int? PriceTypical = null,
    int? PriceMax = null,
    decimal Factor = 1m);

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
    public Task<EstimateMeasurementDto> HandleAsync(MeasureEstimate query, CancellationToken cancellationToken) =>
        EstimateCalculator.CalculateAsync(db, language, query.Rooms, query.OldBuilding, skipUnavailable: false, cancellationToken);
}
