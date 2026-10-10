using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimateSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "old_building",
                table: "estimates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "share_token",
                table: "estimates",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "estimate_line",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_estimates_share_token",
                table: "estimates",
                column: "share_token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_estimates_share_token",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "old_building",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "share_token",
                table: "estimates");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "estimate_line");
        }
    }
}
