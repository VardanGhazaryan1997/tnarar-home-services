using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "partner_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_from = table.Column<int>(type: "integer", nullable: false),
                    price_to = table.Column<int>(type: "integer", nullable: true),
                    includes_materials = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partner_prices", x => x.id);
                    table.CheckConstraint("ck_partner_prices_range", "price_from >= 1 AND (price_to IS NULL OR price_to >= price_from)");
                    table.ForeignKey(
                        name: "fk_partner_prices_partner_profiles_partner_profile_id",
                        column: x => x.partner_profile_id,
                        principalTable: "partner_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_partner_prices_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_partner_prices_partner_profile_id_work_item_id",
                table: "partner_prices",
                columns: new[] { "partner_profile_id", "work_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partner_prices_work_item_id",
                table: "partner_prices",
                column: "work_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_prices");
        }
    }
}
