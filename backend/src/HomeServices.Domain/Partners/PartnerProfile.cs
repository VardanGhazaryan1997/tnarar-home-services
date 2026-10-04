using HomeServices.Domain.Common;

namespace HomeServices.Domain.Partners;

/// <summary>
/// A user's partner profile: what they do, where, and examples of their work. Staff review it
/// before it becomes public. Every status change is kept in <see cref="StatusChanges"/>.
/// </summary>
public sealed class PartnerProfile : AuditableEntity, IAudited
{
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 100;
    public const int AboutMaxLength = 2000;
    public const int AboutMinLengthToSubmit = 50;
    public const int MaxYearsOfExperience = 70;
    public const int MaxServices = 20;
    public const int MaxAreas = 30;
    public const int MaxWorkExamples = 30;
    public const int MaxDocuments = 10;
    public const int CaptionMaxLength = 200;
    public const int SlugMaxLength = 80;

    /// <summary>What <see cref="MissingForSubmit"/> can report.</summary>
    public static class Missing
    {
        public const string About = "about";
        public const string Services = "services";
        public const string Areas = "areas";
        public const string WorkExamples = "work_examples";
    }

    private readonly List<PartnerService> _services = [];
    private readonly List<PartnerArea> _areas = [];
    private readonly List<PartnerMedia> _media = [];
    private readonly List<PartnerStatusChange> _statusChanges = [];

    private PartnerProfile()
    {
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// The public address of the profile, e.g. "aram-santekhnik-3f9a2c": the name in Latin letters plus a
    /// short code from the id, so it is unique. Set once; renaming keeps old links working.
    /// </summary>
    public string? Slug { get; private set; }

    public PartnerType Type { get; private set; }

    public PartnerStatus Status { get; private set; }

    /// <summary>The name customers see: the specialist's or the company's.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    public string About { get; private set; } = string.Empty;

    public int? YearsOfExperience { get; private set; }

    public Guid? AvatarFileId { get; private set; }

    /// <summary>When the profile was last sent for review.</summary>
    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>When the profile was first approved.</summary>
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>Set while a commission statement is long overdue: the partner gets no new requests until they pay.</summary>
    public DateTimeOffset? DebtPausedSince { get; private set; }

    public IReadOnlyCollection<PartnerService> Services => _services.AsReadOnly();

    public IReadOnlyCollection<PartnerArea> Areas => _areas.AsReadOnly();

    public IReadOnlyCollection<PartnerMedia> Media => _media.AsReadOnly();

    public IReadOnlyCollection<PartnerStatusChange> StatusChanges => _statusChanges.AsReadOnly();

    /// <summary>The partner can change the profile: not while it's being reviewed, rejected or suspended. Approved profiles stay public while edited.</summary>
    public bool CanEdit => Status is PartnerStatus.Draft or PartnerStatus.NeedsChanges or PartnerStatus.Approved;

    public bool CanSubmit => Status is PartnerStatus.Draft or PartnerStatus.NeedsChanges && MissingForSubmit().Count == 0;

    /// <summary>The staff comment of the latest "needs changes", rejection or suspension, if it's still the current status.</summary>
    public string? ReviewComment =>
        _statusChanges.MaxBy(c => c.Sequence) is { } latest && latest.ToStatus == Status && Status is PartnerStatus.NeedsChanges or PartnerStatus.Rejected or PartnerStatus.Suspended
            ? latest.Comment
            : null;

    public static PartnerProfile Create(Guid userId, PartnerType type, string displayName)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("partner.user_required", "A partner profile belongs to a user.");
        }

        var profile = new PartnerProfile
        {
            UserId = userId,
            Type = EnsureType(type),
            DisplayName = CleanDisplayName(displayName),
            Status = PartnerStatus.Draft,
        };
        profile.Slug = profile.MakeSlug();
        return profile;
    }

    /// <summary>Changes the profile's details. The type is fixed once the profile has been approved.</summary>
    public void UpdateDetails(PartnerType type, string displayName, string? about, int? yearsOfExperience, Guid? avatarFileId)
    {
        EnsureEditable();
        if (type != Type && ApprovedAt is not null)
        {
            throw new DomainException("partner.type_locked", "The partner type can't change after approval.");
        }

        var cleanAbout = (about ?? string.Empty).Trim();
        if (cleanAbout.Length > AboutMaxLength)
        {
            throw new DomainException("partner.about_too_long", $"About must be at most {AboutMaxLength} characters.");
        }

        if (yearsOfExperience is < 0 or > MaxYearsOfExperience)
        {
            throw new DomainException("partner.years_invalid", $"Years of experience must be between 0 and {MaxYearsOfExperience}.");
        }

        Type = EnsureType(type);
        DisplayName = CleanDisplayName(displayName);
        About = cleanAbout;
        YearsOfExperience = yearsOfExperience;
        AvatarFileId = avatarFileId;
    }

    /// <summary>Replaces the categories the partner offers. The caller checks the categories exist.</summary>
    public void SetServices(IEnumerable<Guid> categoryIds)
    {
        EnsureEditable();
        var wanted = categoryIds.Distinct().ToList();
        if (wanted.Count > MaxServices)
        {
            throw new DomainException("partner.too_many_services", $"At most {MaxServices} services.");
        }

        _services.RemoveAll(s => !wanted.Contains(s.CategoryId));
        foreach (var categoryId in wanted.Where(id => _services.All(s => s.CategoryId != id)))
        {
            _services.Add(new PartnerService(Id, categoryId));
        }
    }

    /// <summary>
    /// Replaces where the partner works. A whole city (no district) covers its districts, so
    /// districts of a city that is also chosen whole are dropped. The caller checks the places exist.
    /// </summary>
    public void SetAreas(IEnumerable<(Guid CityId, Guid? DistrictId)> areas)
    {
        EnsureEditable();
        var distinct = areas.Distinct().ToList();
        var wholeCities = distinct.Where(a => a.DistrictId is null).Select(a => a.CityId).ToHashSet();
        var wanted = distinct.Where(a => a.DistrictId is null || !wholeCities.Contains(a.CityId)).ToList();
        if (wanted.Count > MaxAreas)
        {
            throw new DomainException("partner.too_many_areas", $"At most {MaxAreas} areas.");
        }

        _areas.RemoveAll(a => !wanted.Contains((a.CityId, a.DistrictId)));
        foreach (var (cityId, districtId) in wanted.Where(w => !_areas.Any(a => a.CityId == w.CityId && a.DistrictId == w.DistrictId)))
        {
            _areas.Add(new PartnerArea(Id, cityId, districtId));
        }
    }

    /// <summary>Attaches a stored file. The caller checks the file is ready, the partner's, and of a fitting kind.</summary>
    public PartnerMedia AddMedia(PartnerMediaKind kind, Guid fileId, string? caption)
    {
        EnsureEditable();
        if (!Enum.IsDefined(kind))
        {
            throw new DomainException("partner.media_kind_invalid", "Unknown media kind.");
        }

        if (_media.Any(m => m.FileId == fileId))
        {
            throw new DomainException("partner.media_duplicate", "This file is already on the profile.");
        }

        var ofKind = _media.Where(m => m.Kind == kind).ToList();
        var limit = kind == PartnerMediaKind.WorkExample ? MaxWorkExamples : MaxDocuments;
        if (ofKind.Count >= limit)
        {
            throw new DomainException("partner.too_many_media", $"At most {limit} files of this kind.");
        }

        var cleanCaption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        if (cleanCaption?.Length > CaptionMaxLength)
        {
            throw new DomainException("partner.caption_too_long", $"Captions must be at most {CaptionMaxLength} characters.");
        }

        var media = new PartnerMedia(Id, kind, fileId, cleanCaption, ofKind.Count == 0 ? 1 : ofKind.Max(m => m.SortOrder) + 1);
        _media.Add(media);
        return media;
    }

    public void RemoveMedia(Guid mediaId)
    {
        EnsureEditable();
        if (_media.RemoveAll(m => m.Id == mediaId) == 0)
        {
            throw new DomainException("partner.media_not_found", "No such file on the profile.");
        }
    }

    /// <summary>What still has to be filled in before the profile can be submitted (see <see cref="Missing"/>).</summary>
    public IReadOnlyList<string> MissingForSubmit()
    {
        var missing = new List<string>();
        if (About.Length < AboutMinLengthToSubmit)
        {
            missing.Add(Missing.About);
        }

        if (_services.Count == 0)
        {
            missing.Add(Missing.Services);
        }

        if (_areas.Count == 0)
        {
            missing.Add(Missing.Areas);
        }

        if (_media.All(m => m.Kind != PartnerMediaKind.WorkExample))
        {
            missing.Add(Missing.WorkExamples);
        }

        return missing;
    }

    /// <summary>The partner sends the profile for review (first time, or after making requested changes).</summary>
    public void Submit(DateTimeOffset now)
    {
        if (Status is not (PartnerStatus.Draft or PartnerStatus.NeedsChanges))
        {
            throw new DomainException("partner.cannot_submit", $"A profile that is {Status} can't be submitted.");
        }

        if (MissingForSubmit().Count > 0)
        {
            throw new DomainException("partner.incomplete", $"Still missing: {string.Join(", ", MissingForSubmit())}.");
        }

        SubmittedAt = now;
        ChangeStatus(PartnerStatus.UnderReview, comment: null);
    }

    /// <summary>Staff approve the profile; it becomes public.</summary>
    public void Approve(DateTimeOffset now)
    {
        EnsureStatus(PartnerStatus.UnderReview, "partner.not_under_review");
        ApprovedAt ??= now;
        Slug ??= MakeSlug();
        ChangeStatus(PartnerStatus.Approved, comment: null);
    }

    /// <summary>Staff send the profile back with what to fix.</summary>
    public void RequestChanges(string comment)
    {
        EnsureStatus(PartnerStatus.UnderReview, "partner.not_under_review");
        ChangeStatus(PartnerStatus.NeedsChanges, RequireComment(comment));
    }

    public void Reject(string comment)
    {
        EnsureStatus(PartnerStatus.UnderReview, "partner.not_under_review");
        ChangeStatus(PartnerStatus.Rejected, RequireComment(comment));
    }

    /// <summary>Staff hide an approved partner (complaints, expired documents…).</summary>
    public void Suspend(string comment)
    {
        EnsureStatus(PartnerStatus.Approved, "partner.not_approved");
        ChangeStatus(PartnerStatus.Suspended, RequireComment(comment));
    }

    public void Reinstate()
    {
        EnsureStatus(PartnerStatus.Suspended, "partner.not_suspended");
        ChangeStatus(PartnerStatus.Approved, comment: null);
    }

    /// <summary>Stops new requests because commissions are long overdue. Returns false when already paused.</summary>
    public bool PauseForDebt(DateTimeOffset now)
    {
        if (DebtPausedSince is not null)
        {
            return false;
        }

        DebtPausedSince = now;
        return true;
    }

    /// <summary>Lets requests through again once nothing is long overdue. Returns false when it wasn't paused.</summary>
    public bool ResumeAfterDebt()
    {
        if (DebtPausedSince is null)
        {
            return false;
        }

        DebtPausedSince = null;
        return true;
    }

    // The name part keeps the slug readable; the id part keeps it unique.
    private string MakeSlug()
    {
        var code = Id.ToString("N")[^6..];
        var name = Transliteration.ToSlug(DisplayName, SlugMaxLength - code.Length - 1);
        return name.Length == 0 ? $"partner-{code}" : $"{name}-{code}";
    }

    private void ChangeStatus(PartnerStatus to, string? comment)
    {
        var sequence = _statusChanges.Count == 0 ? 1 : _statusChanges.Max(c => c.Sequence) + 1;
        _statusChanges.Add(new PartnerStatusChange(Id, sequence, Status, to, comment));
        Status = to;
    }

    private void EnsureEditable()
    {
        if (!CanEdit)
        {
            throw new DomainException("partner.not_editable", $"A profile that is {Status} can't be changed.");
        }
    }

    private void EnsureStatus(PartnerStatus expected, string code)
    {
        if (Status != expected)
        {
            throw new DomainException(code, $"The profile is {Status}, not {expected}.");
        }
    }

    private static string RequireComment(string? comment)
    {
        var clean = comment?.Trim() ?? string.Empty;
        if (clean.Length == 0)
        {
            throw new DomainException("partner.comment_required", "Tell the partner why.");
        }

        return clean.Length > PartnerStatusChange.CommentMaxLength
            ? throw new DomainException("partner.comment_too_long", $"Comments must be at most {PartnerStatusChange.CommentMaxLength} characters.")
            : clean;
    }

    private static PartnerType EnsureType(PartnerType type) =>
        Enum.IsDefined(type) ? type : throw new DomainException("partner.type_invalid", "Unknown partner type.");

    private static string CleanDisplayName(string? displayName)
    {
        var clean = displayName?.Trim() ?? string.Empty;
        return clean.Length is >= DisplayNameMinLength and <= DisplayNameMaxLength
            ? clean
            : throw new DomainException("partner.display_name_invalid", $"The name must be {DisplayNameMinLength}–{DisplayNameMaxLength} characters.");
    }
}
