using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Commissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "debt_paused_since",
                table: "partner_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "commission_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_rates_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_statements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    total = table.Column<int>(type: "integer", nullable: false),
                    paid_amount = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_on = table.Column<DateOnly>(type: "date", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    overdue_reminder_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_statements_partner_profiles_partner_profile_id",
                        column: x => x.partner_profile_id,
                        principalTable: "partner_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_obligations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_price = table.Column<int>(type: "integer", nullable: false),
                    rate_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commission_obligations", x => x.id);
                    table.ForeignKey(
                        name: "fk_commission_obligations_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commission_obligations_commission_statements_statement_id",
                        column: x => x.statement_id,
                        principalTable: "commission_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commission_obligations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_commission_obligations_partner_profiles_partner_profile_id",
                        column: x => x.partner_profile_id,
                        principalTable: "partner_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "settlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_settlements_commission_statements_statement_id",
                        column: x => x.statement_id,
                        principalTable: "commission_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_settlements_partner_profiles_partner_profile_id",
                        column: x => x.partner_profile_id,
                        principalTable: "partner_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "commission_rates",
                columns: new[] { "id", "category_id", "percent" },
                values: new object[] { new Guid("019a0000-0000-7000-8000-000000000901"), null, 10m });

            migrationBuilder.CreateIndex(
                name: "ix_commission_obligations_category_id",
                table: "commission_obligations",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_obligations_order_id",
                table: "commission_obligations",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_obligations_partner_profile_id_statement_id",
                table: "commission_obligations",
                columns: new[] { "partner_profile_id", "statement_id" });

            migrationBuilder.CreateIndex(
                name: "ix_commission_obligations_statement_id",
                table: "commission_obligations",
                column: "statement_id");

            migrationBuilder.CreateIndex(
                name: "ix_commission_rates_category_id",
                table: "commission_rates",
                column: "category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_statements_partner_profile_id_period_start",
                table: "commission_statements",
                columns: new[] { "partner_profile_id", "period_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_statements_status_due_on",
                table: "commission_statements",
                columns: new[] { "status", "due_on" });

            migrationBuilder.CreateIndex(
                name: "ix_settlements_partner_profile_id",
                table: "settlements",
                column: "partner_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_statement_id",
                table: "settlements",
                column: "statement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_obligations");

            migrationBuilder.DropTable(
                name: "commission_rates");

            migrationBuilder.DropTable(
                name: "settlements");

            migrationBuilder.DropTable(
                name: "commission_statements");

            migrationBuilder.DropColumn(
                name: "debt_paused_since",
                table: "partner_profiles");
        }
    }
}
