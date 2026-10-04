using HomeServices.Application.Errors;
using HomeServices.Application.Staff;
using HomeServices.Application.Tests.Support;
using HomeServices.Domain;
using HomeServices.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Application.Tests.Staff;

public class RoleManagementTests
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly Role _operator = Role.Create("Operator", "Reviews partners", [Permissions.PartnersView], isSystem: true);
    private readonly Role _custom = Role.Create("Night shift", "Evenings", [Permissions.RequestsView]);

    public RoleManagementTests()
    {
        _db.Roles.AddRange(_operator, _custom);
        _db.SaveChanges();
    }

    [Fact]
    public async Task A_role_is_created_with_its_permissions()
    {
        var role = await new CreateRoleHandler(_db).HandleAsync(
            new CreateRole(" Quality ", null, [Permissions.ReviewsModerate, Permissions.PartnersView, Permissions.PartnersView]),
            CancellationToken.None);

        role.Name.ShouldBe("Quality");
        role.Description.ShouldBeEmpty();
        role.IsSystem.ShouldBeFalse();
        role.Permissions.ShouldBe(new[] { Permissions.PartnersView, Permissions.ReviewsModerate });
        (await _db.Roles.CountAsync()).ShouldBe(3);
    }

    [Fact]
    public async Task Role_names_are_unique_ignoring_case()
    {
        var error = await Should.ThrowAsync<ConflictException>(() =>
            new CreateRoleHandler(_db).HandleAsync(new CreateRole("OPERATOR", null, []), CancellationToken.None));

        error.Code.ShouldBe("role.name_taken");
        (await Should.ThrowAsync<ConflictException>(() =>
            new UpdateRoleHandler(_db).HandleAsync(new UpdateRole(_custom.Id, "operator", null, []), CancellationToken.None)))
            .Code.ShouldBe("role.name_taken");
    }

    [Fact]
    public async Task A_custom_role_is_renamed_and_re_permissioned()
    {
        var role = await new UpdateRoleHandler(_db).HandleAsync(
            new UpdateRole(_custom.Id, "Night support", "22:00–06:00", [Permissions.SupportManage]),
            CancellationToken.None);

        role.Name.ShouldBe("Night support");
        role.Description.ShouldBe("22:00–06:00");
        role.Permissions.ShouldBe(new[] { Permissions.SupportManage });
    }

    [Fact]
    public async Task Built_in_roles_keep_their_name_but_their_permissions_change()
    {
        var handler = new UpdateRoleHandler(_db);

        var updated = await handler.HandleAsync(
            new UpdateRole(_operator.Id, " Operator ", "Partners and requests", [Permissions.PartnersView, Permissions.RequestsManage]),
            CancellationToken.None);

        updated.Description.ShouldBe("Partners and requests");
        updated.Permissions.ShouldBe(new[] { Permissions.PartnersView, Permissions.RequestsManage });
        (await Should.ThrowAsync<DomainException>(() =>
            handler.HandleAsync(new UpdateRole(_operator.Id, "Operators", null, []), CancellationToken.None)))
            .Code.ShouldBe("role.system_cannot_be_renamed");
    }

    [Fact]
    public async Task Only_unused_custom_roles_are_deleted()
    {
        var handler = new DeleteRoleHandler(_db);
        var staff = StaffUser.Create("ani@example.com", "Ani", "pw:x");
        staff.AssignRole(_custom.Id);
        _db.StaffUsers.Add(staff);
        await _db.SaveChangesAsync();

        (await Should.ThrowAsync<DomainException>(() => handler.HandleAsync(new DeleteRole(_operator.Id), CancellationToken.None)))
            .Code.ShouldBe("role.system_cannot_be_deleted");
        (await Should.ThrowAsync<ConflictException>(() => handler.HandleAsync(new DeleteRole(_custom.Id), CancellationToken.None)))
            .Code.ShouldBe("role.in_use");

        staff.RemoveRole(_custom.Id);
        await _db.SaveChangesAsync();
        (await handler.HandleAsync(new DeleteRole(_custom.Id), CancellationToken.None)).ShouldBeTrue();
        (await _db.Roles.AnyAsync(r => r.Id == _custom.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Unknown_roles_are_404()
    {
        (await Should.ThrowAsync<NotFoundException>(() =>
            new UpdateRoleHandler(_db).HandleAsync(new UpdateRole(Guid.NewGuid(), "X", null, []), CancellationToken.None)))
            .Code.ShouldBe("role.not_found");
        (await Should.ThrowAsync<NotFoundException>(() =>
            new DeleteRoleHandler(_db).HandleAsync(new DeleteRole(Guid.NewGuid()), CancellationToken.None)))
            .Code.ShouldBe("role.not_found");
    }

    [Fact]
    public void Role_fields_are_validated()
    {
        var validator = new CreateRoleValidator();

        validator.Validate(new CreateRole("Quality", "Checks reviews", [Permissions.ReviewsModerate])).IsValid.ShouldBeTrue();
        validator.Validate(new CreateRole(" ", new string('d', Role.DescriptionMaxLength + 1), ["nope"]))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "name.required", "description.too_long", "permissions.unknown" }, ignoreOrder: true);
        validator.Validate(new CreateRole(new string('n', Role.NameMaxLength + 1), null, [Permissions.RolesManage]))
            .Errors.Select(e => e.ErrorCode)
            .ShouldBe(new[] { "name.too_long", "permissions.super_admin_only" }, ignoreOrder: true);
        new UpdateRoleValidator().Validate(new UpdateRole(Guid.NewGuid(), "Quality", null, null!))
            .Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe("permissions.required");
    }
}
