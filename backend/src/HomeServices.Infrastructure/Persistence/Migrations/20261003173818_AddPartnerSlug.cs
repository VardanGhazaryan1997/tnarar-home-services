using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_partner_profiles_status",
                table: "partner_profiles");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "partner_profiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_profiles_slug",
                table: "partner_profiles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_profiles_status_approved_at",
                table: "partner_profiles",
                columns: new[] { "status", "approved_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_partner_profiles_slug",
                table: "partner_profiles");

            migrationBuilder.DropIndex(
                name: "ix_partner_profiles_status_approved_at",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "partner_profiles");

            migrationBuilder.CreateIndex(
                name: "ix_partner_profiles_status",
                table: "partner_profiles",
                column: "status");
        }
    }
}
