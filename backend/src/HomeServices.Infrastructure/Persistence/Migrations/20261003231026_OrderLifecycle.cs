using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrderLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "auto_complete_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancel_reason",
                table: "orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cancelled_by",
                table: "orders",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completed_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "completion_requested_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "needs_attention_since",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "changed_by",
                table: "order_status_changes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "order_status_changes",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_change_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposed_by = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount = table.Column<int>(type: "integer", nullable: true),
                    new_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    new_duration_days = table.Column<int>(type: "integer", nullable: true),
                    new_visit_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    response_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    proposed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_change_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_change_requests_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_auto_complete_at",
                table: "orders",
                column: "auto_complete_at",
                filter: "status = 'CompletionRequested'");

            migrationBuilder.CreateIndex(
                name: "ix_orders_needs_attention_since",
                table: "orders",
                column: "needs_attention_since",
                filter: "needs_attention_since IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_status_created_at",
                table: "orders",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_order_change_requests_one_pending",
                table: "order_change_requests",
                column: "order_id",
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_order_change_requests_order_id_proposed_at",
                table: "order_change_requests",
                columns: new[] { "order_id", "proposed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_change_requests");

            migrationBuilder.DropIndex(
                name: "ix_orders_auto_complete_at",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_needs_attention_since",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "ix_orders_status_created_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "auto_complete_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancel_reason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_by",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "completed_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "completion_requested_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "needs_attention_since",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "changed_by",
                table: "order_status_changes");

            migrationBuilder.DropColumn(
                name: "note",
                table: "order_status_changes");
        }
    }
}
