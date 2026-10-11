using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_price_locked",
                table: "materials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "market_budget",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "market_premium",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "market_standard",
                table: "materials",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "market_store_count",
                table: "materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "market_updated_at",
                table: "materials",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    logo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stores", x => x.id);
                    table.ForeignKey(
                        name: "fk_stores_files_logo_file_id",
                        column: x => x.logo_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "store_branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    district_id = table.Column<Guid>(type: "uuid", nullable: true),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    opening_hours = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_branches", x => x.id);
                    table.ForeignKey(
                        name: "fk_store_branches_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_store_branches_districts_district_id",
                        column: x => x.district_id,
                        principalTable: "districts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_store_branches_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    material_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    pack_size = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    pack_label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    price = table.Column<int>(type: "integer", nullable: false),
                    sale_price = table.Column<int>(type: "integer", nullable: true),
                    sale_ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    in_stock = table.Column<bool>(type: "boolean", nullable: false),
                    photo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    price_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_products", x => x.id);
                    table.CheckConstraint("ck_store_products_grade", "material_id IS NULL OR grade IS NOT NULL");
                    table.CheckConstraint("ck_store_products_price", "price >= 1 AND pack_size > 0");
                    table.CheckConstraint("ck_store_products_sale", "(sale_price IS NULL AND sale_ends_on IS NULL) OR (sale_price >= 1 AND sale_price < price AND sale_ends_on IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_store_products_files_photo_file_id",
                        column: x => x.photo_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_store_products_materials_material_id",
                        column: x => x.material_id,
                        principalTable: "materials",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_store_products_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("0aeacc79-15b8-5762-86ce-c2e7b77e52b3"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("13bc3b02-b92a-55be-b637-e0ea5c42ae30"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("1d10ccf9-9dd3-5958-9030-01c8f0d34bb1"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("22eff1e5-9a80-5f6b-b7a2-0f5862f253a4"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("239124a2-fd0c-512e-8409-3b9203fc6c77"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("277e41c0-9250-58b6-b555-e4a7ebc413b8"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("2c95d25a-b5e4-5b65-a172-c330c73cadbe"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("3104e53e-e8b0-5f76-aa6b-d9dc838bc460"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("34aac121-8a63-5457-9d9a-b2c12f4843dc"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("35c25029-da0c-57a9-92cc-5cd80833a0f5"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("4998f2c5-d264-572d-863c-bf8e79a43fba"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("50286ae2-f5c5-51fa-a55e-e6deeecd6bb3"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6764950a-64c5-581f-b057-1a580f22602f"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("68727bfc-3cae-5a69-a172-b480d1a2f498"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6b3f09c6-d8ff-5359-a620-c50520a20fde"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6e6800d1-6be4-5b9b-84d9-c297be3a3925"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6f4492c8-bbb1-556a-a081-3651875933f9"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("6f590444-0e90-52c8-876f-762c0a59ec62"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7166237c-8f6c-55eb-b10b-e4a8d3df6aca"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("73507bbd-db34-5567-984f-aeb418897fd6"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("76a7fb3f-d88d-5d74-bf72-2f63a5019df3"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7769fe69-927f-54f9-8490-353d200ccb36"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("7d1b804c-3f8c-5dee-a028-d922fbc183e1"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("8031a834-0e95-58bb-a13c-36d1dc1ff919"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("81a158f9-9e88-549c-b7b6-1a773e515d04"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("81b298a3-1e66-59e3-a8d3-195862de0e5e"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("8741452e-9b8e-56c2-9754-d942c6aa20c3"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("90e64574-fd59-582e-8ad9-c077ab09f8e6"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9233b7ba-8b48-5b0d-9d28-427d65cba9b6"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("92ae4fcc-d7ae-5707-9e52-59e9884233ff"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9df0d7ff-35b8-5b46-b535-ae8355975de1"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("9f8a85e2-b4a3-563e-b05d-c70542f7938e"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("a4e520a7-14ba-5ded-a173-fcbe7eb37831"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("a892ed8e-e82a-56db-87c7-3ab2f40e9d37"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("ac0a19cd-ebab-5363-bbac-7f8551ac5944"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b4a5823c-2b6d-5cac-b371-31248ab4bcaa"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b4d17f95-0902-5520-acd7-3e90b0b1e31f"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b71ba2d4-d5b6-5a4b-855b-528982d342ea"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("b8af3f88-1ddc-5feb-8516-4aad63fe230d"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("c0b00171-923b-5c56-a251-a11c54dfd110"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("c686cc22-4094-5c63-9812-64b1c4eb0894"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("d7cd4f47-0a39-5e40-ae71-f0466d977788"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("de366b65-a1f2-5736-a287-e9fdb66260b2"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("e0509cca-a68a-5af0-97e8-245988a814ab"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("e45888d9-64df-57b8-98c2-c376b94bacf9"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("fb023651-b621-5824-861e-f60b41cdd155"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "materials",
                keyColumn: "id",
                keyValue: new Guid("fb27f958-9446-55a1-8bd6-2aff47914509"),
                columns: new[] { "is_price_locked", "market_budget", "market_premium", "market_standard", "market_store_count", "market_updated_at" },
                values: new object[] { false, null, null, null, 0, null });

            migrationBuilder.CreateIndex(
                name: "ix_store_branches_city_id",
                table: "store_branches",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_branches_district_id",
                table: "store_branches",
                column: "district_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_branches_store_id_sort_order",
                table: "store_branches",
                columns: new[] { "store_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_store_products_material_id",
                table: "store_products",
                column: "material_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_photo_file_id",
                table: "store_products",
                column: "photo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_store_products_store_id_code",
                table: "store_products",
                columns: new[] { "store_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stores_is_active_sort_order",
                table: "stores",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_stores_logo_file_id",
                table: "stores",
                column: "logo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_stores_slug",
                table: "stores",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_branches");

            migrationBuilder.DropTable(
                name: "store_products");

            migrationBuilder.DropTable(
                name: "stores");

            migrationBuilder.DropColumn(
                name: "is_price_locked",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "market_budget",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "market_premium",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "market_standard",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "market_store_count",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "market_updated_at",
                table: "materials");
        }
    }
}
