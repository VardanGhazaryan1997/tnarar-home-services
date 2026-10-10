using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estimates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimates", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimates_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_estimates_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estimate_room",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estimate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    length = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    width = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    area = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    height = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimate_room", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimate_room_estimates_estimate_id",
                        column: x => x.estimate_id,
                        principalTable: "estimates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "estimate_line",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimate_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimate_line_estimate_room_room_id",
                        column: x => x.room_id,
                        principalTable: "estimate_room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_estimate_line_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "estimate_opening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    width = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    height = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estimate_opening", x => x.id);
                    table.ForeignKey(
                        name: "fk_estimate_opening_estimate_room_room_id",
                        column: x => x.room_id,
                        principalTable: "estimate_room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_estimate_line_room_id_work_item_id",
                table: "estimate_line",
                columns: new[] { "room_id", "work_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estimate_line_work_item_id",
                table: "estimate_line",
                column: "work_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_estimate_opening_room_id",
                table: "estimate_opening",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "ix_estimate_room_estimate_id",
                table: "estimate_room",
                column: "estimate_id");

            migrationBuilder.CreateIndex(
                name: "ix_estimates_city_id",
                table: "estimates",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_estimates_user_id",
                table: "estimates",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estimate_line");

            migrationBuilder.DropTable(
                name: "estimate_opening");

            migrationBuilder.DropTable(
                name: "estimate_room");

            migrationBuilder.DropTable(
                name: "estimates");
        }
    }
}
