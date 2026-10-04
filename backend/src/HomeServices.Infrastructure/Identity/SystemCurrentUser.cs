using HomeServices.Application.Abstractions;

namespace HomeServices.Infrastructure.Identity;

/// <summary>No signed-in user (startup tasks, background jobs): every change is a system change. The API replaces it per request.</summary>
internal sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public bool IsStaff => false;
}
