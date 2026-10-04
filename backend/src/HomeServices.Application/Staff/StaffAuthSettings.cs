namespace HomeServices.Application.Staff;

/// <summary>Back Office sign-in rules (configuration section "StaffAuth").</summary>
public sealed class StaffAuthSettings
{
    public const string SectionName = "StaffAuth";

    public int MaxFailedSignIns { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    /// <summary>Staff sessions are shorter than Portal sessions.</summary>
    public int RefreshTokenLifetimeHours { get; set; } = 12;

    public int MinPasswordLength { get; set; } = 12;

    /// <summary>How long an invitation link works.</summary>
    public int InviteLifetimeDays { get; set; } = 7;
}
