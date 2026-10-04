using HomeServices.Application.Files;
using HomeServices.Domain.Files;
using HomeServices.Domain.Partners;

namespace HomeServices.Application.Partners;

public sealed record PartnerAreaDto(Guid CityId, Guid? DistrictId);

public sealed record PartnerMediaDto(Guid Id, string? Caption, int SortOrder, FileDto File);

/// <summary>
/// A partner profile as its owner (and staff) see it. <see cref="Slug"/> is its public address once approved. <see cref="MissingForSubmit"/> lists what
/// still blocks submitting: "about", "services", "areas", "work_examples".
/// </summary>
public sealed record PartnerProfileDto(
    Guid Id,
    string? Slug,
    string Type,
    string Status,
    string DisplayName,
    string About,
    int? YearsOfExperience,
    FileDto? Avatar,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<PartnerAreaDto> Areas,
    IReadOnlyList<PartnerMediaDto> WorkExamples,
    IReadOnlyList<PartnerMediaDto> Documents,
    bool CanEdit,
    bool CanSubmit,
    IReadOnlyList<string> MissingForSubmit,
    string? ReviewComment,
    DateTimeOffset? SubmittedAt);

/// <summary>Builds <see cref="PartnerProfileDto"/>s, with fresh signed links for the files.</summary>
public sealed class PartnerProfileDtoFactory(FileDtoFactory files)
{
    public async Task<PartnerProfileDto> CreateAsync(
        PartnerProfile profile,
        IReadOnlyDictionary<Guid, StoredFile> storedFiles,
        CancellationToken cancellationToken)
    {
        FileDto? avatar = null;
        if (profile.AvatarFileId is { } avatarId && storedFiles.TryGetValue(avatarId, out var avatarFile))
        {
            avatar = await files.CreateAsync(avatarFile, cancellationToken);
        }

        var media = new List<(PartnerMediaKind Kind, PartnerMediaDto Dto)>();
        foreach (var item in profile.Media.OrderBy(m => m.SortOrder))
        {
            if (storedFiles.TryGetValue(item.FileId, out var file))
            {
                media.Add((item.Kind, new PartnerMediaDto(item.Id, item.Caption, item.SortOrder, await files.CreateAsync(file, cancellationToken))));
            }
        }

        return new PartnerProfileDto(
            profile.Id,
            profile.Slug,
            profile.Type.ToString(),
            profile.Status.ToString(),
            profile.DisplayName,
            profile.About,
            profile.YearsOfExperience,
            avatar,
            profile.Services.Select(s => s.CategoryId).Order().ToList(),
            profile.Areas.Select(a => new PartnerAreaDto(a.CityId, a.DistrictId)).OrderBy(a => a.CityId).ThenBy(a => a.DistrictId).ToList(),
            media.Where(m => m.Kind == PartnerMediaKind.WorkExample).Select(m => m.Dto).ToList(),
            media.Where(m => m.Kind == PartnerMediaKind.Document).Select(m => m.Dto).ToList(),
            profile.CanEdit,
            profile.CanSubmit,
            profile.MissingForSubmit(),
            profile.ReviewComment,
            profile.SubmittedAt);
    }
}
