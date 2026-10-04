namespace HomeServices.Application.Abstractions;

/// <summary>The signed-in user for the current request, or null for system actions.</summary>
public interface ICurrentUser
{
    string? UserId { get; }

    /// <summary>True when <see cref="UserId"/> is a Back Office staff member rather than a Portal user.</summary>
    bool IsStaff { get; }
}
