using System.Buffers.Text;
using System.Security.Cryptography;
using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Estimates;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Estimates;

/// <summary>The signed-in user's saved estimates, newest first, with their totals.</summary>
public sealed record GetMyEstimates : IQuery<IReadOnlyList<EstimateSummaryDto>>;

/// <summary>One of the user's estimates with its rooms and prices; 404 "estimate.not_found" for anyone else's.</summary>
public sealed record GetMyEstimate(Guid Id) : IQuery<EstimateDto>;

/// <summary>
/// Saves an estimate: a new one when <paramref name="Id"/> is null, otherwise replaces the title, place, building age and
/// all rooms of that estimate with the ones sent.
/// </summary>
public sealed record SaveMyEstimate(Guid? Id, string Title, Guid? CityId, bool OldBuilding, IReadOnlyList<EstimateRoomInput> Rooms) : ICommand<EstimateDto>;

public sealed record DeleteMyEstimate(Guid Id) : ICommand<bool>;

/// <summary>Turns the estimate's share link on (a new secret link) or off.</summary>
public sealed record ShareMyEstimate(Guid Id, bool Share) : ICommand<EstimateDto>;

/// <summary>A shared estimate by its link, for anyone who has the link (read-only, no owner details).</summary>
public sealed record GetSharedEstimate(string Token) : IQuery<SharedEstimateDto>;

/// <summary>A room as the customer edits it: length × width, or only the floor area; height defaults to 2.7 m.</summary>
public sealed record EstimateRoomInput(
    string Name,
    RoomType Type,
    decimal? Length,
    decimal? Width,
    decimal? Area,
    decimal? Height,
    IReadOnlyList<OpeningInput>? Openings = null,
    IReadOnlyList<LineInput>? Lines = null);

public sealed record EstimateSummaryDto(
    Guid Id,
    string Title,
    int RoomCount,
    int TotalMin,
    int TotalTypical,
    int TotalMax,
    bool IsShared,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// A saved estimate. <see cref="Measurement"/> has a room per room (same order) and a line per line whose work is still
/// offered: a line of the room missing there is work that has since been removed from the catalog.
/// </summary>
public sealed record EstimateDto(
    Guid Id,
    string Title,
    Guid? CityId,
    bool OldBuilding,
    string? ShareToken,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<EstimateRoomDto> Rooms,
    EstimateMeasurementDto Measurement);

public sealed record EstimateRoomDto(
    Guid Id,
    string Name,
    RoomType Type,
    decimal? Length,
    decimal? Width,
    decimal? Area,
    decimal Height,
    IReadOnlyList<OpeningInput> Openings,
    IReadOnlyList<LineInput> Lines);

public sealed record SharedEstimateDto(
    string Title,
    Guid? CityId,
    bool OldBuilding,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<EstimateRoomDto> Rooms,
    EstimateMeasurementDto Measurement);

public sealed class SaveMyEstimateValidator : AbstractValidator<SaveMyEstimate>
{
    public SaveMyEstimateValidator()
    {
        RuleFor(x => x.Title).Must(title => !string.IsNullOrWhiteSpace(title) && title.Trim().Length <= Estimate.TitleMaxLength).WithErrorCode("title.invalid");
        RuleFor(x => x.Rooms).NotNull().WithErrorCode("rooms.required")
            .Must(rooms => rooms is null || rooms.Count <= Estimate.MaxRooms).WithErrorCode("rooms.count_invalid");
        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.Name).Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= EstimateRoom.NameMaxLength)
                .WithErrorCode("room.name_invalid");
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

public sealed class GetMyEstimatesHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language)
    : IQueryHandler<GetMyEstimates, IReadOnlyList<EstimateSummaryDto>>
{
    public async Task<IReadOnlyList<EstimateSummaryDto>> HandleAsync(GetMyEstimates query, CancellationToken cancellationToken)
    {
        var userId = MyEstimateData.UserId(currentUser);
        var estimates = await MyEstimateData.WithRooms(db.Estimates.AsNoTracking())
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.UpdatedAt ?? e.CreatedAt)
            .Take(MyEstimateData.MaxPerUser)
            .ToListAsync(cancellationToken);

        var result = new List<EstimateSummaryDto>(estimates.Count);
        foreach (var estimate in estimates)
        {
            var measurement = await MyEstimateData.MeasureAsync(db, language, estimate, cancellationToken);
            result.Add(new EstimateSummaryDto(
                estimate.Id,
                estimate.Title,
                estimate.Rooms.Count,
                measurement.TotalMin,
                measurement.TotalTypical,
                measurement.TotalMax,
                estimate.ShareToken is not null,
                estimate.CreatedAt,
                estimate.UpdatedAt));
        }

        return result;
    }
}

public sealed class GetMyEstimateHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language) : IQueryHandler<GetMyEstimate, EstimateDto>
{
    public async Task<EstimateDto> HandleAsync(GetMyEstimate query, CancellationToken cancellationToken)
    {
        var estimate = await MyEstimateData.LoadAsync(db.Estimates.AsNoTracking(), currentUser, query.Id, cancellationToken);
        return await MyEstimateData.ToDtoAsync(db, language, estimate, cancellationToken);
    }
}

public sealed class SaveMyEstimateHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language, TimeProvider clock)
    : ICommandHandler<SaveMyEstimate, EstimateDto>
{
    public async Task<EstimateDto> HandleAsync(SaveMyEstimate command, CancellationToken cancellationToken)
    {
        var userId = MyEstimateData.UserId(currentUser);
        if (command.CityId is { } cityId && !await db.Cities.AnyAsync(c => c.Id == cityId, cancellationToken))
        {
            throw new DomainException("estimate.city_not_found", "This town is no longer in the list.");
        }

        Estimate estimate;
        if (command.Id is { } id)
        {
            estimate = await MyEstimateData.LoadAsync(db.Estimates, currentUser, id, cancellationToken);
            estimate.Rename(command.Title);
            estimate.MoveTo(command.CityId);
            estimate.SetOldBuilding(command.OldBuilding);
            estimate.ClearRooms();
            estimate.MarkUpdated(clock.GetUtcNow(), currentUser.UserId); // also when only rooms changed
        }
        else
        {
            if (await db.Estimates.CountAsync(e => e.UserId == userId, cancellationToken) >= MyEstimateData.MaxPerUser)
            {
                throw new DomainException("estimate.too_many", $"You can keep up to {MyEstimateData.MaxPerUser} estimates. Delete an old one first.");
            }

            estimate = Estimate.Create(userId, command.Title, command.CityId, command.OldBuilding);
            db.Estimates.Add(estimate);
        }

        await AddRoomsAsync(estimate, command.Rooms, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await MyEstimateData.ToDtoAsync(db, language, estimate, cancellationToken);
    }

    // Lines must be for work in the catalog. Hidden work is allowed (a line kept from before the work was hidden); prices
    // leave it out.
    private async Task AddRoomsAsync(Estimate estimate, IReadOnlyList<EstimateRoomInput> rooms, CancellationToken cancellationToken)
    {
        var ids = rooms.SelectMany(r => r.Lines ?? Array.Empty<LineInput>()).Select(l => l.WorkItemId).Distinct().ToList();
        var existing = await db.WorkItems.Where(w => ids.Contains(w.Id)).Select(w => w.Id).ToListAsync(cancellationToken);
        if (existing.Count != ids.Count)
        {
            throw new DomainException("estimate.work_item_not_found", "Some of the work is no longer offered. Refresh the page.");
        }

        foreach (var input in rooms)
        {
            var size = new RoomSize(input.Length, input.Width, input.Area, input.Height ?? RoomSize.DefaultHeight);
            var openings = (input.Openings ?? Array.Empty<OpeningInput>()).Select(o => new RoomOpening(o.Kind, o.Width, o.Height, o.Count));
            var room = estimate.AddRoom(input.Name, input.Type, size, openings);
            foreach (var line in input.Lines ?? Array.Empty<LineInput>())
            {
                room.AddLine(line.WorkItemId, line.Quantity);
            }
        }
    }
}

public sealed class DeleteMyEstimateHandler(IAppDbContext db, ICurrentUser currentUser) : ICommandHandler<DeleteMyEstimate, bool>
{
    public async Task<bool> HandleAsync(DeleteMyEstimate command, CancellationToken cancellationToken)
    {
        var estimate = await MyEstimateData.LoadAsync(db.Estimates, currentUser, command.Id, cancellationToken);
        db.Estimates.Remove(estimate);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed class ShareMyEstimateHandler(IAppDbContext db, ICurrentUser currentUser, ICurrentLanguage language) : ICommandHandler<ShareMyEstimate, EstimateDto>
{
    public async Task<EstimateDto> HandleAsync(ShareMyEstimate command, CancellationToken cancellationToken)
    {
        var estimate = await MyEstimateData.LoadAsync(db.Estimates, currentUser, command.Id, cancellationToken);
        if (command.Share)
        {
            estimate.Share(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(16)));
        }
        else
        {
            estimate.StopSharing();
        }

        await db.SaveChangesAsync(cancellationToken);
        return await MyEstimateData.ToDtoAsync(db, language, estimate, cancellationToken);
    }
}

public sealed class GetSharedEstimateHandler(IAppDbContext db, ICurrentLanguage language) : IQueryHandler<GetSharedEstimate, SharedEstimateDto>
{
    public async Task<SharedEstimateDto> HandleAsync(GetSharedEstimate query, CancellationToken cancellationToken)
    {
        var estimate = string.IsNullOrEmpty(query.Token) || query.Token.Length > Estimate.ShareTokenMaxLength
            ? null
            : await MyEstimateData.WithRooms(db.Estimates.AsNoTracking()).SingleOrDefaultAsync(e => e.ShareToken == query.Token, cancellationToken);
        if (estimate is null)
        {
            throw new NotFoundException("This link no longer works.", "estimate.not_found");
        }

        var dto = await MyEstimateData.ToDtoAsync(db, language, estimate, cancellationToken);
        return new SharedEstimateDto(dto.Title, dto.CityId, dto.OldBuilding, dto.UpdatedAt ?? dto.CreatedAt, dto.Rooms, dto.Measurement);
    }
}

internal static class MyEstimateData
{
    /// <summary>How many estimates a user can keep.</summary>
    public const int MaxPerUser = 50;

    /// <summary>The Portal user's id; staff and visitors who aren't signed in get 401.</summary>
    public static Guid UserId(ICurrentUser currentUser) =>
        !currentUser.IsStaff && Guid.TryParse(currentUser.UserId, out var id)
            ? id
            : throw new UnauthorizedException("Please sign in.");

    public static IQueryable<Estimate> WithRooms(IQueryable<Estimate> estimates) =>
        estimates
            .Include(e => e.Rooms).ThenInclude(r => r.Openings)
            .Include(e => e.Rooms).ThenInclude(r => r.Lines);

    public static async Task<Estimate> LoadAsync(IQueryable<Estimate> estimates, ICurrentUser currentUser, Guid id, CancellationToken cancellationToken)
    {
        var userId = UserId(currentUser);
        return await WithRooms(estimates).SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("This estimate doesn't exist.", "estimate.not_found");
    }

    public static async Task<EstimateDto> ToDtoAsync(IAppDbContext db, ICurrentLanguage language, Estimate estimate, CancellationToken cancellationToken)
    {
        var rooms = Rooms(estimate);
        var measurement = await MeasureAsync(db, language, estimate, cancellationToken);
        return new EstimateDto(
            estimate.Id,
            estimate.Title,
            estimate.CityId,
            estimate.OldBuilding,
            estimate.ShareToken,
            estimate.CreatedAt,
            estimate.UpdatedAt,
            rooms,
            measurement);
    }

    public static Task<EstimateMeasurementDto> MeasureAsync(IAppDbContext db, ICurrentLanguage language, Estimate estimate, CancellationToken cancellationToken) =>
        EstimateCalculator.CalculateAsync(
            db,
            language,
            Rooms(estimate).Select(r => new RoomInput(r.Type, r.Length, r.Width, r.Area, r.Height, r.Openings, r.Lines)).ToList(),
            estimate.OldBuilding,
            skipUnavailable: true,
            cancellationToken);

    private static List<EstimateRoomDto> Rooms(Estimate estimate) =>
        estimate.Rooms
            .OrderBy(r => r.SortOrder)
            .Select(r => new EstimateRoomDto(
                r.Id,
                r.Name,
                r.Type,
                r.Length,
                r.Width,
                r.Area,
                r.Height,
                r.Openings.Select(o => new OpeningInput(o.Kind, o.Width, o.Height, o.Count)).ToList(),
                r.Lines.OrderBy(l => l.SortOrder).Select(l => new LineInput(l.WorkItemId, l.Quantity)).ToList()))
            .ToList();
}
