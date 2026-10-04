using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain.Files;
using HomeServices.Domain.Identity;
using HomeServices.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Partners;

/// <summary>The signed-in user's partner profile.</summary>
public sealed record GetMyPartnerProfile : IQuery<PartnerProfileDto>;

/// <summary>
/// Creates the signed-in user's partner profile, or changes it. Services and areas are replaced
/// by the lists sent. Creating it gives the user the Partner role.
/// </summary>
public sealed record SaveMyPartnerProfile(
    PartnerType Type,
    string DisplayName,
    string? About,
    int? YearsOfExperience,
    Guid? AvatarFileId,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<PartnerAreaDto> Areas) : ICommand<PartnerProfileDto>;

/// <summary>Attaches an uploaded file as a work example or document.</summary>
public sealed record AddMyPartnerMedia(PartnerMediaKind Kind, Guid FileId, string? Caption) : ICommand<PartnerProfileDto>;

public sealed record RemoveMyPartnerMedia(Guid MediaId) : ICommand<PartnerProfileDto>;

/// <summary>Sends the profile for review.</summary>
public sealed record SubmitMyPartnerProfile : ICommand<PartnerProfileDto>;

public sealed class SaveMyPartnerProfileValidator : AbstractValidator<SaveMyPartnerProfile>
{
    public SaveMyPartnerProfileValidator(IAppDbContext db, ICurrentUser currentUser)
    {
        RuleFor(x => x.Type).IsInEnum().WithErrorCode("type.invalid");
        RuleFor(x => x.DisplayName)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("display_name.required")
            .Must(name => name.Trim().Length is >= PartnerProfile.DisplayNameMinLength and <= PartnerProfile.DisplayNameMaxLength)
            .WithErrorCode("display_name.length")
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName), ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.About)
            .Must(about => about!.Trim().Length <= PartnerProfile.AboutMaxLength).WithErrorCode("about.too_long")
            .When(x => x.About is not null);
        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, PartnerProfile.MaxYearsOfExperience).WithErrorCode("years_of_experience.invalid");

        RuleFor(x => x.CategoryIds).NotNull().WithErrorCode("categories.required");
        RuleFor(x => x.CategoryIds)
            .Must(ids => ids.Distinct().Count() <= PartnerProfile.MaxServices).WithErrorCode("categories.too_many")
            .MustAsync(async (ids, ct) =>
            {
                var wanted = ids.Distinct().ToList();
                var active = await db.Categories.CountAsync(c => wanted.Contains(c.Id) && c.IsActive, ct);
                return active == wanted.Count;
            })
            .WithErrorCode("categories.invalid")
            .When(x => x.CategoryIds is not null);

        RuleFor(x => x.Areas).NotNull().WithErrorCode("areas.required");
        RuleFor(x => x.Areas)
            .Must(areas => areas.Distinct().Count() <= PartnerProfile.MaxAreas).WithErrorCode("areas.too_many")
            .MustAsync((areas, ct) => AreasExistAsync(db, areas, ct)).WithErrorCode("areas.invalid")
            .When(x => x.Areas is not null);

        RuleFor(x => x.AvatarFileId)
            .MustAsync(async (fileId, ct) => await PartnerFiles.FindUsableAsync(db, currentUser, fileId!.Value, ct) is { Kind: FileKind.Image })
            .WithErrorCode("avatar.invalid")
            .When(x => x.AvatarFileId is not null);
    }

    // Each area is a region, or an active town or village with an optional district that is active and in it.
    private static async Task<bool> AreasExistAsync(IAppDbContext db, IReadOnlyList<PartnerAreaDto> areas, CancellationToken cancellationToken)
    {
        if (areas.Any(a => a is null || !new AreaChoice(a.RegionId, a.CityId, a.DistrictId).IsValid))
        {
            return false;
        }

        var regionIds = areas.Where(a => a.RegionId is not null).Select(a => a.RegionId!.Value).Distinct().ToList();
        if (await db.Regions.CountAsync(r => regionIds.Contains(r.Id), cancellationToken) != regionIds.Count)
        {
            return false;
        }

        var cityIds = areas.Where(a => a.CityId is not null).Select(a => a.CityId!.Value).Distinct().ToList();
        var cities = await db.Cities.AsNoTracking()
            .Include(c => c.Districts)
            .Where(c => cityIds.Contains(c.Id) && c.IsActive)
            .ToListAsync(cancellationToken);

        return areas.Where(a => a.CityId is not null).All(area => cities.FirstOrDefault(c => c.Id == area.CityId) is { } city
            && (area.DistrictId is not { } districtId || city.Districts.Any(d => d.Id == districtId && d.IsActive)));
    }
}

public sealed class AddMyPartnerMediaValidator : AbstractValidator<AddMyPartnerMedia>
{
    public AddMyPartnerMediaValidator(IAppDbContext db, ICurrentUser currentUser)
    {
        RuleFor(x => x.Kind).IsInEnum().WithErrorCode("kind.invalid");
        RuleFor(x => x.Caption)
            .Must(caption => caption!.Trim().Length <= PartnerProfile.CaptionMaxLength).WithErrorCode("caption.too_long")
            .When(x => x.Caption is not null);
        RuleFor(x => x.FileId)
            .MustAsync(async (command, fileId, ct) =>
                await PartnerFiles.FindUsableAsync(db, currentUser, fileId, ct) is { } file && PartnerFiles.Fits(command.Kind, file.Kind))
            .WithErrorCode("file.invalid")
            .When(x => Enum.IsDefined(x.Kind));
    }
}

public sealed class GetMyPartnerProfileHandler(IAppDbContext db, ICurrentUser currentUser, PartnerProfileDtoFactory dtos)
    : IQueryHandler<GetMyPartnerProfile, PartnerProfileDto>
{
    public async Task<PartnerProfileDto> HandleAsync(GetMyPartnerProfile query, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken)
            ?? throw MyPartnerProfile.NotFound();
        return await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

public sealed class SaveMyPartnerProfileHandler(IAppDbContext db, ICurrentUser currentUser, PartnerProfileDtoFactory dtos)
    : ICommandHandler<SaveMyPartnerProfile, PartnerProfileDto>
{
    public async Task<PartnerProfileDto> HandleAsync(SaveMyPartnerProfile command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken);
        if (profile is null)
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Id == MyPartnerProfile.UserId(currentUser), cancellationToken)
                ?? throw new UnauthorizedException("Please sign in.");
            profile = PartnerProfile.Create(user.Id, command.Type, command.DisplayName);
            user.AddRole(UserRoles.Partner);
            db.PartnerProfiles.Add(profile);
        }

        profile.UpdateDetails(command.Type, command.DisplayName, command.About, command.YearsOfExperience, command.AvatarFileId);
        profile.SetServices(command.CategoryIds);
        profile.SetAreas(await PartnerAreas.ToChoicesAsync(db, command.Areas, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
        return await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

public sealed class AddMyPartnerMediaHandler(IAppDbContext db, ICurrentUser currentUser, PartnerProfileDtoFactory dtos)
    : ICommandHandler<AddMyPartnerMedia, PartnerProfileDto>
{
    public async Task<PartnerProfileDto> HandleAsync(AddMyPartnerMedia command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken)
            ?? throw MyPartnerProfile.NotFound();
        profile.AddMedia(command.Kind, command.FileId, command.Caption);
        await db.SaveChangesAsync(cancellationToken);
        return await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

public sealed class RemoveMyPartnerMediaHandler(IAppDbContext db, ICurrentUser currentUser, PartnerProfileDtoFactory dtos)
    : ICommandHandler<RemoveMyPartnerMedia, PartnerProfileDto>
{
    public async Task<PartnerProfileDto> HandleAsync(RemoveMyPartnerMedia command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken)
            ?? throw MyPartnerProfile.NotFound();
        profile.RemoveMedia(command.MediaId);
        await db.SaveChangesAsync(cancellationToken);
        return await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

public sealed class SubmitMyPartnerProfileHandler(IAppDbContext db, ICurrentUser currentUser, PartnerProfileDtoFactory dtos, TimeProvider clock)
    : ICommandHandler<SubmitMyPartnerProfile, PartnerProfileDto>
{
    public async Task<PartnerProfileDto> HandleAsync(SubmitMyPartnerProfile command, CancellationToken cancellationToken)
    {
        var profile = await MyPartnerProfile.LoadAsync(db, currentUser, cancellationToken)
            ?? throw MyPartnerProfile.NotFound();
        profile.Submit(clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
        return await MyPartnerProfile.ToDtoAsync(db, dtos, profile, cancellationToken);
    }
}

internal static class MyPartnerProfile
{
    /// <summary>The Portal user's id. Staff tokens never reach these endpoints; a missing id means not signed in.</summary>
    public static Guid UserId(ICurrentUser currentUser) =>
        !currentUser.IsStaff && Guid.TryParse(currentUser.UserId, out var id)
            ? id
            : throw new UnauthorizedException("Please sign in.");

    public static async Task<PartnerProfile?> LoadAsync(IAppDbContext db, ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var userId = UserId(currentUser);
        return await db.PartnerProfiles
            .Include(p => p.Services)
            .Include(p => p.Areas)
            .Include(p => p.Media)
            .Include(p => p.StatusChanges)
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public static NotFoundException NotFound() => new("You don't have a partner profile yet.", "partner.not_found");

    public static async Task<PartnerProfileDto> ToDtoAsync(IAppDbContext db, PartnerProfileDtoFactory dtos, PartnerProfile profile, CancellationToken cancellationToken)
    {
        var fileIds = profile.Media.Select(m => m.FileId).ToList();
        if (profile.AvatarFileId is { } avatarId)
        {
            fileIds.Add(avatarId);
        }

        var files = await db.Files.AsNoTracking().Where(f => fileIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        return await dtos.CreateAsync(profile, files, cancellationToken);
    }
}

internal static class PartnerAreas
{
    /// <summary>
    /// The chosen areas as <see cref="AreaChoice"/>s. A whole region covers its towns and villages, so places inside a
    /// chosen region are dropped.
    /// </summary>
    public static async Task<List<AreaChoice>> ToChoicesAsync(IAppDbContext db, IReadOnlyList<PartnerAreaDto> areas, CancellationToken cancellationToken)
    {
        var regionIds = areas.Where(a => a.RegionId is not null).Select(a => a.RegionId!.Value).ToHashSet();
        var cityIds = areas.Where(a => a.CityId is not null).Select(a => a.CityId!.Value).Distinct().ToList();
        var cityRegions = regionIds.Count == 0
            ? new Dictionary<Guid, Guid?>()
            : await db.Cities.AsNoTracking()
                .Where(c => cityIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.RegionId, cancellationToken);

        return areas
            .Where(a => a.CityId is not { } cityId
                || !(cityRegions.TryGetValue(cityId, out var regionId) && regionId is { } r && regionIds.Contains(r)))
            .Select(a => new AreaChoice(a.RegionId, a.CityId, a.DistrictId))
            .ToList();
    }
}

internal static class PartnerFiles
{
    /// <summary>A ready file uploaded by the signed-in Portal user, or null.</summary>
    public static async Task<StoredFile?> FindUsableAsync(IAppDbContext db, ICurrentUser currentUser, Guid fileId, CancellationToken cancellationToken)
    {
        if (currentUser.IsStaff || currentUser.UserId is not { } userId)
        {
            return null;
        }

        return await db.Files.AsNoTracking().SingleOrDefaultAsync(
            f => f.Id == fileId && f.OwnerType == FileOwnerType.User && f.OwnerId == userId && f.Status == FileStatus.Ready,
            cancellationToken);
    }

    /// <summary>Work examples are photos or videos; documents are photos (scans) or PDFs.</summary>
    public static bool Fits(PartnerMediaKind kind, FileKind fileKind) => kind switch
    {
        PartnerMediaKind.WorkExample => fileKind is FileKind.Image or FileKind.Video,
        _ => fileKind is FileKind.Image or FileKind.Document,
    };
}
