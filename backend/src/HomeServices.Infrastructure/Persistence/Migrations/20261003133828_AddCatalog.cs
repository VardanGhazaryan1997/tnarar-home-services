using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    icon = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "districts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_districts", x => x.id);
                    table.ForeignKey(
                        name: "fk_districts_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "created_at", "created_by", "deleted_at", "deleted_by", "icon", "is_active", "is_deleted", "name", "parent_id", "slug", "sort_order", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000101"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "brick", true, false, "{\"hy\":\"\\u0547\\u056B\\u0576\\u0561\\u0580\\u0561\\u0580\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576\",\"ru\":\"\\u0421\\u0442\\u0440\\u043E\\u0438\\u0442\\u0435\\u043B\\u044C\\u0441\\u0442\\u0432\\u043E\",\"en\":\"Construction\"}", null, "construction", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000102"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "roller", true, false, "{\"hy\":\"\\u054E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442\",\"en\":\"Renovation\"}", null, "renovation", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000103"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "pipe", true, false, "{\"hy\":\"\\u054D\\u0561\\u0576\\u057F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\",\"ru\":\"\\u0421\\u0430\\u043D\\u0442\\u0435\\u0445\\u043D\\u0438\\u043A\\u0430\",\"en\":\"Plumbing\"}", null, "plumbing", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000104"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "flame", true, false, "{\"hy\":\"\\u054B\\u0565\\u057C\\u0578\\u0582\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0442\\u043E\\u043F\\u043B\\u0435\\u043D\\u0438\\u0435\",\"en\":\"Heating\"}", null, "heating", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000105"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "bolt", true, false, "{\"hy\":\"\\u0537\\u056C\\u0565\\u056F\\u057F\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u042D\\u043B\\u0435\\u043A\\u0442\\u0440\\u043E\\u043C\\u043E\\u043D\\u0442\\u0430\\u0436\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Electrical work\"}", null, "electrical", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000106"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "facade", true, false, "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u056B \\u0565\\u0580\\u0565\\u057D\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u043B\\u0438\\u0446\\u043E\\u0432\\u043A\\u0430 \\u0444\\u0430\\u0441\\u0430\\u0434\\u0430\",\"en\":\"Exterior cladding\"}", null, "exterior-cladding", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000107"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "sparkle", true, false, "{\"hy\":\"\\u054A\\u0580\\u0578\\u0586\\u0565\\u057D\\u056B\\u0578\\u0576\\u0561\\u056C \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u0440\\u043E\\u0444\\u0435\\u0441\\u0441\\u0438\\u043E\\u043D\\u0430\\u043B\\u044C\\u043D\\u0430\\u044F \\u0443\\u0431\\u043E\\u0440\\u043A\\u0430\",\"en\":\"Professional cleaning\"}", null, "cleaning", 7, null, null }
                });

            migrationBuilder.InsertData(
                table: "cities",
                columns: new[] { "id", "is_active", "name", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0535\\u0580\\u0587\\u0561\\u0576\",\"ru\":\"\\u0415\\u0440\\u0435\\u0432\\u0430\\u043D\",\"en\":\"Yerevan\"}", "yerevan", 1 },
                    { new Guid("019a0000-0000-7000-8000-000000000202"), true, "{\"hy\":\"\\u0537\\u057B\\u0574\\u056B\\u0561\\u056E\\u056B\\u0576\",\"ru\":\"\\u042D\\u0447\\u043C\\u0438\\u0430\\u0434\\u0437\\u0438\\u043D\",\"en\":\"Ejmiatsin\"}", "ejmiatsin", 2 },
                    { new Guid("019a0000-0000-7000-8000-000000000203"), true, "{\"hy\":\"\\u0531\\u0562\\u0578\\u057E\\u0575\\u0561\\u0576\",\"ru\":\"\\u0410\\u0431\\u043E\\u0432\\u044F\\u043D\",\"en\":\"Abovyan\"}", "abovyan", 3 },
                    { new Guid("019a0000-0000-7000-8000-000000000204"), true, "{\"hy\":\"\\u0531\\u0577\\u057F\\u0561\\u0580\\u0561\\u056F\",\"ru\":\"\\u0410\\u0448\\u0442\\u0430\\u0440\\u0430\\u043A\",\"en\":\"Ashtarak\"}", "ashtarak", 4 },
                    { new Guid("019a0000-0000-7000-8000-000000000205"), true, "{\"hy\":\"\\u0544\\u0561\\u057D\\u056B\\u057D\",\"ru\":\"\\u041C\\u0430\\u0441\\u0438\\u0441\",\"en\":\"Masis\"}", "masis", 5 }
                });

            migrationBuilder.InsertData(
                table: "districts",
                columns: new[] { "id", "city_id", "is_active", "name", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000301"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0531\\u057B\\u0561\\u0583\\u0576\\u0575\\u0561\\u056F\",\"ru\":\"\\u0410\\u0447\\u0430\\u043F\\u043D\\u044F\\u043A\",\"en\":\"Ajapnyak\"}", "ajapnyak", 1 },
                    { new Guid("019a0000-0000-7000-8000-000000000302"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0531\\u0580\\u0561\\u0562\\u056F\\u056B\\u0580\",\"ru\":\"\\u0410\\u0440\\u0430\\u0431\\u043A\\u0438\\u0440\",\"en\":\"Arabkir\"}", "arabkir", 2 },
                    { new Guid("019a0000-0000-7000-8000-000000000303"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0531\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0432\\u0430\\u043D\",\"en\":\"Avan\"}", "avan", 3 },
                    { new Guid("019a0000-0000-7000-8000-000000000304"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0534\\u0561\\u057E\\u0569\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0414\\u0430\\u0432\\u0442\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Davtashen\"}", "davtashen", 4 },
                    { new Guid("019a0000-0000-7000-8000-000000000305"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0537\\u0580\\u0565\\u0562\\u0578\\u0582\\u0576\\u056B\",\"ru\":\"\\u042D\\u0440\\u0435\\u0431\\u0443\\u043D\\u0438\",\"en\":\"Erebuni\"}", "erebuni", 5 },
                    { new Guid("019a0000-0000-7000-8000-000000000306"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0554\\u0561\\u0576\\u0561\\u0584\\u0565\\u057C-\\u0536\\u0565\\u0575\\u0569\\u0578\\u0582\\u0576\",\"ru\":\"\\u041A\\u0430\\u043D\\u0430\\u043A\\u0435\\u0440-\\u0417\\u0435\\u0439\\u0442\\u0443\\u043D\",\"en\":\"Kanaker-Zeytun\"}", "kanaker-zeytun", 6 },
                    { new Guid("019a0000-0000-7000-8000-000000000307"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u053F\\u0565\\u0576\\u057F\\u0580\\u0578\\u0576\",\"ru\":\"\\u041A\\u0435\\u043D\\u0442\\u0440\\u043E\\u043D\",\"en\":\"Kentron\"}", "kentron", 7 },
                    { new Guid("019a0000-0000-7000-8000-000000000308"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0544\\u0561\\u056C\\u0561\\u0569\\u056B\\u0561-\\u054D\\u0565\\u0562\\u0561\\u057D\\u057F\\u056B\\u0561\",\"ru\":\"\\u041C\\u0430\\u043B\\u0430\\u0442\\u0438\\u044F-\\u0421\\u0435\\u0431\\u0430\\u0441\\u0442\\u0438\\u044F\",\"en\":\"Malatia-Sebastia\"}", "malatia-sebastia", 8 },
                    { new Guid("019a0000-0000-7000-8000-000000000309"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0546\\u0578\\u0580\\u0584\",\"ru\":\"\\u041D\\u043E\\u0440 \\u041D\\u043E\\u0440\\u043A\",\"en\":\"Nor Nork\"}", "nor-nork", 9 },
                    { new Guid("019a0000-0000-7000-8000-000000000310"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0546\\u0578\\u0580\\u0584-\\u0544\\u0561\\u0580\\u0561\\u0577\",\"ru\":\"\\u041D\\u043E\\u0440\\u043A-\\u041C\\u0430\\u0440\\u0430\\u0448\",\"en\":\"Nork-Marash\"}", "nork-marash", 10 },
                    { new Guid("019a0000-0000-7000-8000-000000000311"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0546\\u0578\\u0582\\u0562\\u0561\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041D\\u0443\\u0431\\u0430\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Nubarashen\"}", "nubarashen", 11 },
                    { new Guid("019a0000-0000-7000-8000-000000000312"), new Guid("019a0000-0000-7000-8000-000000000201"), true, "{\"hy\":\"\\u0547\\u0565\\u0576\\u0563\\u0561\\u057E\\u056B\\u0569\",\"ru\":\"\\u0428\\u0435\\u043D\\u0433\\u0430\\u0432\\u0438\\u0442\",\"en\":\"Shengavit\"}", "shengavit", 12 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_categories_slug",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cities_slug",
                table: "cities",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_districts_city_id_slug",
                table: "districts",
                columns: new[] { "city_id", "slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "districts");

            migrationBuilder.DropTable(
                name: "cities");
        }
    }
}
