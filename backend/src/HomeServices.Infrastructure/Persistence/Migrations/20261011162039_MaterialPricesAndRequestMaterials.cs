using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterialPricesAndRequestMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "material_grade",
                table: "service_requests",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "materials_amount",
                table: "offers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "prices_checked_at",
                table: "materials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.CreateTable(
                name: "request_materials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    estimate_cost = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request_materials", x => x.id);
                    table.ForeignKey(
                        name: "fk_request_materials_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_request_materials_service_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "service_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("0aeacc79-15b8-5762-86ce-c2e7b77e52b3"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("13bc3b02-b92a-55be-b637-e0ea5c42ae30"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("1d10ccf9-9dd3-5958-9030-01c8f0d34bb1"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("22eff1e5-9a80-5f6b-b7a2-0f5862f253a4"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("239124a2-fd0c-512e-8409-3b9203fc6c77"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("277e41c0-9250-58b6-b555-e4a7ebc413b8"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("2c95d25a-b5e4-5b65-a172-c330c73cadbe"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("3104e53e-e8b0-5f76-aa6b-d9dc838bc460"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("34aac121-8a63-5457-9d9a-b2c12f4843dc"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("35c25029-da0c-57a9-92cc-5cd80833a0f5"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("4998f2c5-d264-572d-863c-bf8e79a43fba"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("50286ae2-f5c5-51fa-a55e-e6deeecd6bb3"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6764950a-64c5-581f-b057-1a580f22602f"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("68727bfc-3cae-5a69-a172-b480d1a2f498"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6b3f09c6-d8ff-5359-a620-c50520a20fde"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6e6800d1-6be4-5b9b-84d9-c297be3a3925"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6f4492c8-bbb1-556a-a081-3651875933f9"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6f590444-0e90-52c8-876f-762c0a59ec62"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7166237c-8f6c-55eb-b10b-e4a8d3df6aca"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("73507bbd-db34-5567-984f-aeb418897fd6"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("76a7fb3f-d88d-5d74-bf72-2f63a5019df3"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7769fe69-927f-54f9-8490-353d200ccb36"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7d1b804c-3f8c-5dee-a028-d922fbc183e1"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("8031a834-0e95-58bb-a13c-36d1dc1ff919"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("81a158f9-9e88-549c-b7b6-1a773e515d04"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("81b298a3-1e66-59e3-a8d3-195862de0e5e"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("8741452e-9b8e-56c2-9754-d942c6aa20c3"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("90e64574-fd59-582e-8ad9-c077ab09f8e6"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9233b7ba-8b48-5b0d-9d28-427d65cba9b6"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("92ae4fcc-d7ae-5707-9e52-59e9884233ff"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9df0d7ff-35b8-5b46-b535-ae8355975de1"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9f8a85e2-b4a3-563e-b05d-c70542f7938e"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("a4e520a7-14ba-5ded-a173-fcbe7eb37831"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("a892ed8e-e82a-56db-87c7-3ab2f40e9d37"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("ac0a19cd-ebab-5363-bbac-7f8551ac5944"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b4a5823c-2b6d-5cac-b371-31248ab4bcaa"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b4d17f95-0902-5520-acd7-3e90b0b1e31f"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b71ba2d4-d5b6-5a4b-855b-528982d342ea"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b8af3f88-1ddc-5feb-8516-4aad63fe230d"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("c0b00171-923b-5c56-a251-a11c54dfd110"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("c686cc22-4094-5c63-9812-64b1c4eb0894"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("d7cd4f47-0a39-5e40-ae71-f0466d977788"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("de366b65-a1f2-5736-a287-e9fdb66260b2"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("e0509cca-a68a-5af0-97e8-245988a814ab"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("e45888d9-64df-57b8-98c2-c376b94bacf9"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("fb023651-b621-5824-861e-f60b41cdd155"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("fb27f958-9446-55a1-8bd6-2aff47914509"),
                column: "prices_checked_at",
                value: new DateTimeOffset(new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "ix_request_materials_material_id",
                table: "request_materials",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_materials_request_id_sort_order",
                table: "request_materials",
                columns: new[] { "request_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_materials");

            migrationBuilder.DropColumn(
                name: "material_grade",
                table: "service_requests");

            migrationBuilder.DropColumn(
                name: "materials_amount",
                table: "offers");

            migrationBuilder.DropColumn(
                name: "prices_checked_at",
                table: "materials");
        }
    }
}
