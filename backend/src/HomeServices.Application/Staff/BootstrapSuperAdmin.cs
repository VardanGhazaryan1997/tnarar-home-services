using HomeServices.Application.Abstractions;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HomeServices.Application.Staff;

/// <summary>
/// Creates the first Super Admin when no staff account exists yet (run at startup from configuration).
/// Does nothing once any staff account exists, so the configured password is only used once.
/// </summary>
public sealed record BootstrapSuperAdmin(string Email, string FullName, string Password) : ICommand<bool>;

public sealed class BootstrapSuperAdminHandler(IAppDbContext db, IPasswordHasher passwords, IOptions<StaffAuthSettings> options)
    : ICommandHandler<BootstrapSuperAdmin, bool>
{
    public async Task<bool> HandleAsync(BootstrapSuperAdmin command, CancellationToken cancellationToken)
    {
        if (await db.StaffUsers.AnyAsync(cancellationToken))
        {
            return false;
        }

        if (command.Password.Length < options.Value.MinPasswordLength)
        {
            throw new DomainException("staff.password_too_short", $"The password must be at least {options.Value.MinPasswordLength} characters.");
        }

        db.StaffUsers.Add(StaffUser.Create(command.Email, command.FullName, passwords.Hash(command.Password), isSuperAdmin: true));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
