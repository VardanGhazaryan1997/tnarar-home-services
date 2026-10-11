namespace HomeServices.Application.Stores;

/// <summary>How store prices become material prices (configuration section "StorePricing").</summary>
public sealed class StorePricingSettings
{
    public const string SectionName = "StorePricing";

    /// <summary>A material's price for a grade comes from stores once at least this many different stores sell it.</summary>
    public int MinStores { get; set; } = 2;

    /// <summary>A store price counts for this many days after it was set or last confirmed; then it is "stale".</summary>
    public int FreshDays { get; set; } = 90;

    /// <summary>Hours from UTC to the stores' local time (Armenia: 4), to tell when a sale ends.</summary>
    public int UtcOffsetHours { get; set; } = 4;

    public TimeSpan FreshFor => TimeSpan.FromDays(FreshDays);

    /// <summary>Today's date where the stores are.</summary>
    public DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(UtcOffsetHours)).DateTime);
}
