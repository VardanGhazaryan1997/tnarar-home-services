using HomeServices.Domain.Common;

namespace HomeServices.Domain.Partners;

/// <summary>A category of work the partner offers.</summary>
public sealed class PartnerService : IAudited
{
    private PartnerService()
    {
    }

    internal PartnerService(Guid partnerProfileId, Guid categoryId)
    {
        PartnerProfileId = partnerProfileId;
        CategoryId = categoryId;
    }

    public Guid PartnerProfileId { get; private set; }

    public Guid CategoryId { get; private set; }
}

/// <summary>Where the partner works: a whole city (no district) or one district of it.</summary>
public sealed class PartnerArea : Entity, IAudited
{
    private PartnerArea()
    {
    }

    internal PartnerArea(Guid partnerProfileId, Guid cityId, Guid? districtId)
    {
        PartnerProfileId = partnerProfileId;
        CityId = cityId;
        DistrictId = districtId;
    }

    public Guid PartnerProfileId { get; private set; }

    public Guid CityId { get; private set; }

    public Guid? DistrictId { get; private set; }
}

/// <summary>A work example or document: a stored file attached to the profile.</summary>
public sealed class PartnerMedia : Entity, IAudited
{
    private PartnerMedia()
    {
    }

    internal PartnerMedia(Guid partnerProfileId, PartnerMediaKind kind, Guid fileId, string? caption, int sortOrder)
    {
        PartnerProfileId = partnerProfileId;
        Kind = kind;
        FileId = fileId;
        Caption = caption;
        SortOrder = sortOrder;
    }

    public Guid PartnerProfileId { get; private set; }

    public PartnerMediaKind Kind { get; private set; }

    public Guid FileId { get; private set; }

    public string? Caption { get; private set; }

    public int SortOrder { get; private set; }
}

/// <summary>
/// One step in the profile's review history: who moved it from which status to which, and why.
/// <see cref="AuditableEntity.CreatedAt"/> and <see cref="AuditableEntity.CreatedBy"/> say when and who.
/// </summary>
public sealed class PartnerStatusChange : AuditableEntity
{
    public const int CommentMaxLength = 2000;

    private PartnerStatusChange()
    {
    }

    internal PartnerStatusChange(Guid partnerProfileId, int sequence, PartnerStatus fromStatus, PartnerStatus toStatus, string? comment)
    {
        PartnerProfileId = partnerProfileId;
        Sequence = sequence;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment;
    }

    public Guid PartnerProfileId { get; private set; }

    /// <summary>1, 2, 3… in the order the changes happened.</summary>
    public int Sequence { get; private set; }

    public PartnerStatus FromStatus { get; private set; }

    public PartnerStatus ToStatus { get; private set; }

    public string? Comment { get; private set; }
}
