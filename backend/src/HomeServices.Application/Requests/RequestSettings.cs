namespace HomeServices.Application.Requests;

/// <summary>How requests reach partners (configuration section "Requests").</summary>
public sealed class RequestSettings
{
    public const string SectionName = "Requests";

    /// <summary>At most this many matching partners receive an open request; more matches are picked at random.</summary>
    public int MaxRecipients { get; set; } = 20;

    /// <summary>A request nobody has responded to this many hours after it was sent goes to the operator queue.</summary>
    public int ResponseHours { get; set; } = 24;

    /// <summary>How often the follow-up job looks for unanswered requests.</summary>
    public int FollowUpMinutes { get; set; } = 15;

    public TimeSpan ResponseWindow => TimeSpan.FromHours(ResponseHours);
}
