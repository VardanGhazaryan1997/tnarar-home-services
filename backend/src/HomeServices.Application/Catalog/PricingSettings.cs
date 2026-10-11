namespace HomeServices.Application.Catalog;

/// <summary>How market prices are worked out from partners' prices (configuration section "Pricing").</summary>
public sealed class PricingSettings
{
    public const string SectionName = "Pricing";

    /// <summary>Partners' prices replace the staff range once at least this many partners priced a work item.</summary>
    public int MinPartners { get; set; } = 5;

    /// <summary>How often the background job recalculates every market range (partners get approved or suspended).</summary>
    public int RecalculateHours { get; set; } = 24;

    /// <summary>
    /// How much more an agreed price counts than a listed one: each line price of an accepted work offer is counted this
    /// many times (C1). 1 = the same as a listed price.
    /// </summary>
    public int AcceptedOfferWeight { get; set; } = 2;

    /// <summary>Accepted offers older than this many days no longer count (prices move).</summary>
    public int AcceptedOfferDays { get; set; } = 365;

    /// <summary>
    /// A partner's price counts (market ranges, public profile) for this many days after they last set or confirmed it
    /// (C3); then they're reminded to confirm.
    /// </summary>
    public int PriceFreshDays { get; set; } = 90;

    /// <summary>A partner can't save a price more than this many times above or below the market's usual price (C4).</summary>
    public int MaxPriceDeviation { get; set; } = 10;

    public TimeSpan PriceFreshFor => TimeSpan.FromDays(PriceFreshDays);
}
