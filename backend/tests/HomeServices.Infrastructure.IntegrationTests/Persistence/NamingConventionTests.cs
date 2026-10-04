using HomeServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeServices.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public class NamingConventionTests(PostgresFixture db)
{
    [Theory]
    [InlineData("Id", "id")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("PartnerProfileId", "partner_profile_id")]
    [InlineData("HTTPStatus", "http_status")]
    [InlineData("Widgets", "widgets")]
    [InlineData("already_snake", "already_snake")]
    [InlineData("", "")]
    public void Names_are_converted_to_snake_case(string name, string expected)
    {
        SnakeCase.Convert(name).ShouldBe(expected);
    }

    [Fact]
    public async Task Tables_and_columns_are_snake_case_in_PostgreSQL()
    {
        await using var context = db.CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>("SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'widgets'")
            .ToListAsync();

        columns.ShouldBe(
            ["id", "display_name", "title", "views", "secret", "created_at", "created_by", "updated_at", "updated_by", "is_deleted", "deleted_at", "deleted_by"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Primary_keys_are_named_in_snake_case()
    {
        await using var context = db.CreateContext();

        var constraints = await context.Database
            .SqlQueryRaw<string>("SELECT constraint_name AS \"Value\" FROM information_schema.table_constraints WHERE table_name = 'widgets' AND constraint_type = 'PRIMARY KEY'")
            .ToListAsync();

        constraints.ShouldBe(["pk_widgets"], ignoreOrder: false);
    }
}
