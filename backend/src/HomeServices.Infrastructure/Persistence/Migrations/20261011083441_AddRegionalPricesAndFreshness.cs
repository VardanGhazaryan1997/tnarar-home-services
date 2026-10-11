using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionalPricesAndFreshness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "prices_reminded_at",
                table: "partner_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "confirmed_at",
                table: "partner_prices",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "regional_market_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    region_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min = table.Column<int>(type: "integer", nullable: false),
                    typical = table.Column<int>(type: "integer", nullable: false),
                    max = table.Column<int>(type: "integer", nullable: false),
                    partner_count = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regional_market_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_regional_market_prices_regions_region_id",
                        column: x => x.region_id,
                        principalTable: "regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_regional_market_prices_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_regional_market_prices_region_id",
                table: "regional_market_prices",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_regional_market_prices_work_item_id_region_id",
                table: "regional_market_prices",
                columns: new[] { "work_item_id", "region_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "regional_market_prices");

            migrationBuilder.DropColumn(
                name: "prices_reminded_at",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "partner_prices");
        }
    }
}
