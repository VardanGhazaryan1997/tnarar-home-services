using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    permissions = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staff_user_role",
                columns: table => new
                {
                    staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_user_role", x => new { x.staff_user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_staff_user_role_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_user_role_staff_users_staff_user_id",
                        column: x => x.staff_user_id,
                        principalTable: "staff_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Built-in roles. Seeded here rather than with HasData because EF compares
            // List<string> seed values by reference and would always report pending changes.
            // Later changes to these roles need a hand-written migration (or the Roles screen).
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "created_by", "description", "is_system", "name", "updated_at", "updated_by", "permissions" },
                columnTypes: new[] { "uuid", "timestamp with time zone", "text", "character varying(256)", "boolean", "character varying(64)", "timestamp with time zone", "text", "text[]" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000401"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Day-to-day administration of the platform", true, "Administrator", null, null, new List<string> { "audit.view", "catalog.manage", "content.manage", "dashboard.view", "orders.manage", "orders.view", "partners.approve", "partners.view", "requests.manage", "requests.view", "reviews.moderate", "staff.view", "users.block", "users.view" } },
                    { new Guid("019a0000-0000-7000-8000-000000000402"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Partners, requests and orders", true, "Operator", null, null, new List<string> { "dashboard.view", "orders.manage", "orders.view", "partners.approve", "partners.view", "requests.manage", "requests.view", "users.view" } },
                    { new Guid("019a0000-0000-7000-8000-000000000403"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Pricing, commissions, debts and settlements", true, "Finance", null, null, new List<string> { "commissions.manage", "commissions.view", "dashboard.view", "materials.pricing", "orders.view", "payments.manage", "payments.view", "reports.view" } },
                    { new Guid("019a0000-0000-7000-8000-000000000404"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Complaints and issue resolution", true, "Support", null, null, new List<string> { "dashboard.view", "orders.view", "requests.view", "reviews.moderate", "support.manage", "users.view" } },
                    { new Guid("019a0000-0000-7000-8000-000000000405"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Categories, pages and translations", true, "Content Manager", null, null, new List<string> { "catalog.manage", "content.manage", "dashboard.view", "translations.manage" } }
                });

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_user_role_role_id",
                table: "staff_user_role",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_user_role");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
