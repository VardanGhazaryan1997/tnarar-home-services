using FluentValidation;
using HomeServices.Application.Abstractions;
using HomeServices.Application.Errors;
using HomeServices.Application.Messaging;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Staff;

/// <summary>
/// The fields of a role. Permission changes reach staff at their next token refresh (within 15 minutes).
/// Super-Admin-only permissions (roles.manage, settings.payments) can't be put in a role.
/// </summary>
public interface IRoleFields
{
    string Name { get; }

    string? Description { get; }

    IReadOnlyList<string> Permissions { get; }
}

public sealed record CreateRole(string Name, string? Description, IReadOnlyList<string> Permissions) : ICommand<RoleDto>, IRoleFields;

/// <summary>Built-in roles keep their name; their description and permissions can change.</summary>
public sealed record UpdateRole(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions) : ICommand<RoleDto>, IRoleFields;

/// <summary>Deletes a role nobody has. Built-in roles can't be deleted.</summary>
public sealed record DeleteRole(Guid Id) : ICommand<bool>;

public abstract class RoleFieldsValidator<T> : AbstractValidator<T>
    where T : IRoleFields
{
    protected RoleFieldsValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithErrorCode("name.required")
            .Must(name => name is null || name.Trim().Length <= Role.NameMaxLength).WithErrorCode("name.too_long");
        RuleFor(x => x.Description)
            .Must(description => description!.Trim().Length <= Role.DescriptionMaxLength).WithErrorCode("description.too_long")
            .When(x => x.Description is not null);
        RuleFor(x => x.Permissions).NotNull().WithErrorCode("permissions.required");
        RuleFor(x => x.Permissions)
            .Must(permissions => permissions.All(Permissions.IsKnown)).WithErrorCode("permissions.unknown")
            .Must(permissions => permissions.Where(Permissions.IsKnown).All(Permissions.IsGrantable)).WithErrorCode("permissions.super_admin_only")
            .When(x => x.Permissions is not null);
    }
}

public sealed class CreateRoleValidator : RoleFieldsValidator<CreateRole>;

public sealed class UpdateRoleValidator : RoleFieldsValidator<UpdateRole>;

public sealed class CreateRoleHandler(IAppDbContext db) : ICommandHandler<CreateRole, RoleDto>
{
    public async Task<RoleDto> HandleAsync(CreateRole command, CancellationToken cancellationToken)
    {
        await RoleRules.EnsureNameFreeAsync(db, command.Name, exceptId: null, cancellationToken);
        var role = Role.Create(command.Name, command.Description ?? string.Empty, command.Permissions);
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return RoleRules.ToDto(role);
    }
}

public sealed class UpdateRoleHandler(IAppDbContext db) : ICommandHandler<UpdateRole, RoleDto>
{
    public async Task<RoleDto> HandleAsync(UpdateRole command, CancellationToken cancellationToken)
    {
        var role = await RoleRules.LoadAsync(db, command.Id, cancellationToken);
        var name = command.Name.Trim();
        if (role.IsSystem && name != role.Name)
        {
            throw new DomainException("role.system_cannot_be_renamed", "Built-in roles can't be renamed.");
        }

        if (!role.IsSystem)
        {
            await RoleRules.EnsureNameFreeAsync(db, name, role.Id, cancellationToken);
            role.Rename(name, command.Description ?? string.Empty);
        }
        else
        {
            role.Describe(command.Description ?? string.Empty);
        }

        role.SetPermissions(command.Permissions);
        await db.SaveChangesAsync(cancellationToken);
        return RoleRules.ToDto(role);
    }
}

public sealed class DeleteRoleHandler(IAppDbContext db) : ICommandHandler<DeleteRole, bool>
{
    public async Task<bool> HandleAsync(DeleteRole command, CancellationToken cancellationToken)
    {
        var role = await RoleRules.LoadAsync(db, command.Id, cancellationToken);
        if (role.IsSystem)
        {
            throw new DomainException("role.system_cannot_be_deleted", "Built-in roles can't be deleted.");
        }

        if (await db.StaffUsers.AnyAsync(s => s.Roles.Any(r => r.RoleId == role.Id), cancellationToken))
        {
            throw new ConflictException("Staff members still have this role. Remove it from them first.", "role.in_use");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}

internal static class RoleRules
{
    public static async Task<Role> LoadAsync(IAppDbContext db, Guid id, CancellationToken cancellationToken) =>
        await db.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw new NotFoundException("Role not found.", "role.not_found");

    // Names are compared ignoring case: "Operator" and "operator" would confuse people.
    public static async Task EnsureNameFreeAsync(IAppDbContext db, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var lowered = name.Trim().ToLowerInvariant();
        if (await db.Roles.AnyAsync(r => r.Name.ToLower() == lowered && r.Id != exceptId, cancellationToken))
        {
            throw new ConflictException("A role with this name already exists.", "role.name_taken");
        }
    }

    public static RoleDto ToDto(Role role) => new(role.Id, role.Name, role.Description, role.IsSystem, role.Permissions);
}
