using HomeServices.Domain.Common;

namespace HomeServices.Domain.Tests.Common;

public class AuditableEntityTests
{
    private sealed class Category : SoftDeletableEntity;

    private static readonly DateTimeOffset Monday = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tuesday = Monday.AddDays(1);

    [Fact]
    public void Records_who_created_it_and_when()
    {
        var category = new Category();

        category.MarkCreated(Monday, "staff-1");

        category.CreatedAt.ShouldBe(Monday);
        category.CreatedBy.ShouldBe("staff-1");
        category.UpdatedAt.ShouldBeNull();
        category.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public void Records_who_last_updated_it_and_when_without_touching_creation()
    {
        var category = new Category();
        category.MarkCreated(Monday, "staff-1");

        category.MarkUpdated(Tuesday, "staff-2");

        category.UpdatedAt.ShouldBe(Tuesday);
        category.UpdatedBy.ShouldBe("staff-2");
        category.CreatedAt.ShouldBe(Monday);
        category.CreatedBy.ShouldBe("staff-1");
    }

    [Fact]
    public void Soft_delete_keeps_the_record_and_remembers_who_deleted_it()
    {
        var category = new Category();

        category.IsDeleted.ShouldBeFalse();

        category.MarkDeleted(Tuesday, "staff-2");

        category.IsDeleted.ShouldBeTrue();
        category.DeletedAt.ShouldBe(Tuesday);
        category.DeletedBy.ShouldBe("staff-2");
    }

    [Fact]
    public void System_changes_have_no_user()
    {
        var category = new Category();

        category.MarkCreated(Monday, null);

        category.CreatedBy.ShouldBeNull();
    }
}
