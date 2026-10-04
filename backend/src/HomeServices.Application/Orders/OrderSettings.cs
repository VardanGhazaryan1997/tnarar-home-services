namespace HomeServices.Application.Orders;

/// <summary>Order lifecycle settings (configuration section "Orders").</summary>
public sealed class OrderSettings
{
    public const string SectionName = "Orders";

    /// <summary>An order the partner marks as done completes by itself if the customer doesn't answer in this many days (fixed when it's marked).</summary>
    public int AutoCompleteDays { get; set; } = 7;

    /// <summary>How often the auto-complete job runs.</summary>
    public int AutoCompleteCheckMinutes { get; set; } = 30;

    public TimeSpan AutoCompleteAfter => TimeSpan.FromDays(AutoCompleteDays);
}
