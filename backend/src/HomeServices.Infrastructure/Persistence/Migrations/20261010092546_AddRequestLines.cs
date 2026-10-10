using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "estimate_id",
                table: "service_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "amount",
                table: "offer_items",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "offer_items",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "request_line_id",
                table: "offer_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "unit_price",
                table: "offer_items",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "request_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    room_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    estimate_min = table.Column<int>(type: "integer", nullable: true),
                    estimate_max = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_request_lines_service_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_request_lines_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_estimate_id",
                table: "service_requests",
                column: "estimate_id");

            migrationBuilder.CreateIndex(
                name: "ix_offer_items_request_line_id",
                table: "offer_items",
                column: "request_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_lines_request_id_sort_order",
                table: "request_lines",
                columns: new[] { "request_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_request_lines_work_item_id",
                table: "request_lines",
                column: "work_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_offer_items_request_lines_request_line_id",
                table: "offer_items",
                column: "request_line_id",
                principalTable: "request_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_service_requests_estimates_estimate_id",
                table: "service_requests",
                column: "estimate_id",
                principalTable: "estimates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_offer_items_request_lines_request_line_id",
                table: "offer_items");

            migrationBuilder.DropForeignKey(
                name: "fk_service_requests_estimates_estimate_id",
                table: "service_requests");

            migrationBuilder.DropTable(
                name: "request_lines");

            migrationBuilder.DropIndex(
                name: "ix_service_requests_estimate_id",
                table: "service_requests");

            migrationBuilder.DropIndex(
                name: "ix_offer_items_request_line_id",
                table: "offer_items");

            migrationBuilder.DropColumn(
                name: "estimate_id",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "amount",
                table: "offer_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "offer_items");

            migrationBuilder.DropColumn(
                name: "request_line_id",
                table: "offer_items");

            migrationBuilder.DropColumn(
                name: "unit_price",
                table: "offer_items");
        }
    }
}
