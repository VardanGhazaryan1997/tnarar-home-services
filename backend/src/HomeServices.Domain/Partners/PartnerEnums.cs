namespace HomeServices.Domain.Partners;

public enum PartnerType
{
    /// <summary>An independent specialist (plumber, electrician…).</summary>
    Specialist = 1,

    /// <summary>A service company with one main manager in release 1.</summary>
    Company = 2,

    /// <summary>A material supplier (material orders arrive in phase 3).</summary>
    Supplier = 3,
}

/// <summary>Draft → Under review → Needs changes / Approved / Rejected; Approved ↔ Suspended. Only Approved partners are public.</summary>
public enum PartnerStatus
{
    Draft = 1,
    UnderReview = 2,
    NeedsChanges = 3,
    Approved = 4,
    Rejected = 5,
    Suspended = 6,
}

public enum PartnerMediaKind
{
    /// <summary>A photo or video of finished work, shown on the public profile.</summary>
    WorkExample = 1,

    /// <summary>An ID, license or company registration. Only staff and the partner see it.</summary>
    Document = 2,
}
