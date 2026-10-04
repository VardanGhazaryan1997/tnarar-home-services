using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategoryTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000102"),
                column: "name",
                value: "{\"hy\":\"\\u054E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574 \\u0587 \\u0570\\u0561\\u0580\\u0564\\u0561\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u0438 \\u043E\\u0442\\u0434\\u0435\\u043B\\u043A\\u0430\",\"en\":\"Renovation and finishing\"}");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000106"),
                column: "name",
                value: "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u0561\\u0575\\u056B\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0424\\u0430\\u0441\\u0430\\u0434\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Facade works\"}");

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "id", "created_at", "created_by", "deleted_at", "deleted_by", "icon", "is_active", "is_deleted", "name", "parent_id", "slug", "sort_order", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000108"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "roof", true, false, "{\"hy\":\"\\u054F\\u0561\\u0576\\u056B\\u0584\\u0561\\u0575\\u056B\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u041A\\u0440\\u043E\\u0432\\u0435\\u043B\\u044C\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Roofing\"}", null, "roofing", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000109"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "window", true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580 \\u0587 \\u0564\\u057C\\u0576\\u0565\\u0580\",\"ru\":\"\\u041E\\u043A\\u043D\\u0430 \\u0438 \\u0434\\u0432\\u0435\\u0440\\u0438\",\"en\":\"Windows and doors\"}", null, "windows-doors", 9, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000110"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "snowflake", true, false, "{\"hy\":\"\\u0555\\u0564\\u0561\\u0583\\u0578\\u056D\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576 \\u0587 \\u0585\\u0564\\u0578\\u0580\\u0561\\u056F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0412\\u0435\\u043D\\u0442\\u0438\\u043B\\u044F\\u0446\\u0438\\u044F \\u0438 \\u043A\\u043E\\u043D\\u0434\\u0438\\u0446\\u0438\\u043E\\u043D\\u0438\\u0440\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435\",\"en\":\"Ventilation and air conditioning\"}", null, "hvac", 10, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000111"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "layers", true, false, "{\"hy\":\"\\u054B\\u0565\\u0580\\u0574\\u0561- \\u0587 \\u057B\\u0580\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0442\\u0435\\u043F\\u043B\\u0435\\u043D\\u0438\\u0435 \\u0438 \\u0433\\u0438\\u0434\\u0440\\u043E\\u0438\\u0437\\u043E\\u043B\\u044F\\u0446\\u0438\\u044F\",\"en\":\"Insulation and waterproofing\"}", null, "insulation", 11, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000112"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "fence", true, false, "{\"hy\":\"\\u0544\\u0565\\u057F\\u0561\\u0572\\u0561\\u056F\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580 \\u0587 \\u0565\\u057C\\u0561\\u056F\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u0435\\u0442\\u0430\\u043B\\u043B\\u043E\\u043A\\u043E\\u043D\\u0441\\u0442\\u0440\\u0443\\u043A\\u0446\\u0438\\u0438 \\u0438 \\u0441\\u0432\\u0430\\u0440\\u043A\\u0430\",\"en\":\"Metalwork and welding\"}", null, "metalwork", 12, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000113"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "hammer", true, false, "{\"hy\":\"\\u0531\\u057F\\u0561\\u0572\\u0571\\u0561\\u0563\\u0578\\u0580\\u056E\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576 \\u0587 \\u056F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584\",\"ru\":\"\\u0421\\u0442\\u043E\\u043B\\u044F\\u0440\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B \\u0438 \\u043C\\u0435\\u0431\\u0435\\u043B\\u044C\",\"en\":\"Carpentry and furniture\"}", null, "carpentry", 13, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000114"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "shovel", true, false, "{\"hy\":\"\\u0540\\u0578\\u0572\\u0561\\u0575\\u056B\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580 \\u0587 \\u0584\\u0561\\u0576\\u0564\\u0578\\u0582\\u0574\",\"ru\":\"\\u0417\\u0435\\u043C\\u043B\\u044F\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B \\u0438 \\u0434\\u0435\\u043C\\u043E\\u043D\\u0442\\u0430\\u0436\",\"en\":\"Earthworks and demolition\"}", null, "earthworks", 14, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000115"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "leaf", true, false, "{\"hy\":\"\\u0532\\u0561\\u056F\\u056B \\u0587 \\u0561\\u0575\\u0563\\u0578\\u0582 \\u0562\\u0561\\u0580\\u0565\\u056F\\u0561\\u0580\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0411\\u043B\\u0430\\u0433\\u043E\\u0443\\u0441\\u0442\\u0440\\u043E\\u0439\\u0441\\u0442\\u0432\\u043E \\u0434\\u0432\\u043E\\u0440\\u0430 \\u0438 \\u0441\\u0430\\u0434\\u0430\",\"en\":\"Landscaping and garden\"}", null, "landscaping", 15, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000116"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "shield", true, false, "{\"hy\":\"\\u0531\\u0576\\u057E\\u057F\\u0561\\u0576\\u0563\\u0578\\u0582\\u0569\\u0575\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580 \\u0587 \\u056D\\u0565\\u056C\\u0561\\u0581\\u056B \\u057F\\u0578\\u0582\\u0576\",\"ru\":\"\\u0421\\u0438\\u0441\\u0442\\u0435\\u043C\\u044B \\u0431\\u0435\\u0437\\u043E\\u043F\\u0430\\u0441\\u043D\\u043E\\u0441\\u0442\\u0438 \\u0438 \\u0443\\u043C\\u043D\\u044B\\u0439 \\u0434\\u043E\\u043C\",\"en\":\"Security and smart home\"}", null, "security-systems", 16, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000117"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "file", true, false, "{\"hy\":\"\\u0546\\u0561\\u056D\\u0561\\u0563\\u056E\\u0578\\u0582\\u0574 \\u0587 \\u0564\\u056B\\u0566\\u0561\\u0575\\u0576\",\"ru\":\"\\u041F\\u0440\\u043E\\u0435\\u043A\\u0442\\u0438\\u0440\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435 \\u0438 \\u0434\\u0438\\u0437\\u0430\\u0439\\u043D\",\"en\":\"Design and engineering\"}", null, "design-engineering", 17, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000000118"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, "tools", true, false, "{\"hy\":\"\\u0544\\u0561\\u0576\\u0580 \\u057F\\u0576\\u0561\\u0575\\u056B\\u0576 \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u0435\\u043B\\u043A\\u0438\\u0439 \\u0431\\u044B\\u0442\\u043E\\u0432\\u043E\\u0439 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442\",\"en\":\"Handyman and small repairs\"}", null, "handyman", 18, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010101"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0561\\u0576 \\u056F\\u0561\\u057C\\u0578\\u0582\\u0581\\u0578\\u0582\\u0574 \\u00AB\\u0562\\u0561\\u0576\\u0561\\u056C\\u056B\\u0576 \\u0571\\u0565\\u057C\\u0584\\u056B\\u0576\\u00BB\",\"ru\":\"\\u0421\\u0442\\u0440\\u043E\\u0438\\u0442\\u0435\\u043B\\u044C\\u0441\\u0442\\u0432\\u043E \\u0434\\u043E\\u043C\\u0430 \\u043F\\u043E\\u0434 \\u043A\\u043B\\u044E\\u0447\",\"en\":\"Turnkey house construction\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-turnkey-houses", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010102"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u056B\\u0574\\u0584\\u056B \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0424\\u0443\\u043D\\u0434\\u0430\\u043C\\u0435\\u043D\\u0442\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Foundations\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-foundations", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010103"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0548\\u0580\\u0574\\u0576\\u0561\\u0564\\u0580\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576 (\\u057F\\u0578\\u0582\\u0586, \\u0562\\u056C\\u0578\\u056F, \\u0561\\u0572\\u0575\\u0578\\u0582\\u057D)\",\"ru\":\"\\u041A\\u043B\\u0430\\u0434\\u043A\\u0430 (\\u0442\\u0443\\u0444, \\u0431\\u043B\\u043E\\u043A, \\u043A\\u0438\\u0440\\u043F\\u0438\\u0447)\",\"en\":\"Masonry (tuff, block, brick)\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-masonry", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010104"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0532\\u0565\\u057F\\u0578\\u0576\\u0561\\u0575\\u056B\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0411\\u0435\\u0442\\u043E\\u043D\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Concrete work\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-concrete", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010105"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0581\\u0561\\u056F\\u0561\\u057C\\u0578\\u0582\\u0575\\u0581\\u0576\\u0565\\u0580 \\u0587 \\u0570\\u0561\\u0580\\u056F\\u056B \\u0561\\u057E\\u0565\\u056C\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u0440\\u0438\\u0441\\u0442\\u0440\\u043E\\u0439\\u043A\\u0438 \\u0438 \\u043D\\u0430\\u0434\\u0441\\u0442\\u0440\\u043E\\u0439\\u043A\\u0438\",\"en\":\"Extensions and added floors\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-extensions", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010106"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u057E\\u057F\\u0578\\u057F\\u0576\\u0561\\u056F\\u0576\\u0565\\u0580 \\u0587 \\u0585\\u056A\\u0561\\u0576\\u0564\\u0561\\u056F \\u0577\\u056B\\u0576\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u0413\\u0430\\u0440\\u0430\\u0436\\u0438 \\u0438 \\u0445\\u043E\\u0437\\u043F\\u043E\\u0441\\u0442\\u0440\\u043E\\u0439\\u043A\\u0438\",\"en\":\"Garages and outbuildings\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-outbuildings", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010107"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u057D\\u057F\\u056B\\u0573\\u0561\\u0576\\u0576\\u0565\\u0580\\u056B \\u056F\\u0561\\u057C\\u0578\\u0582\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0421\\u0442\\u0440\\u043E\\u0438\\u0442\\u0435\\u043B\\u044C\\u0441\\u0442\\u0432\\u043E \\u043B\\u0435\\u0441\\u0442\\u043D\\u0438\\u0446\",\"en\":\"Staircases\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-stairs", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010108"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0561\\u057C\\u0578\\u0582\\u0575\\u0581\\u0576\\u0565\\u0580\\u056B \\u0561\\u0574\\u0580\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0438\\u043B\\u0435\\u043D\\u0438\\u0435 \\u043A\\u043E\\u043D\\u0441\\u0442\\u0440\\u0443\\u043A\\u0446\\u0438\\u0439\",\"en\":\"Structural strengthening\"}", new Guid("019a0000-0000-7000-8000-000000000101"), "construction-reinforcement", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010201"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0532\\u0576\\u0561\\u056F\\u0561\\u0580\\u0561\\u0576\\u056B \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574 \\u00AB\\u0562\\u0561\\u0576\\u0561\\u056C\\u056B\\u0576 \\u0571\\u0565\\u057C\\u0584\\u056B\\u0576\\u00BB\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u043A\\u0432\\u0430\\u0440\\u0442\\u0438\\u0440\\u044B \\u043F\\u043E\\u0434 \\u043A\\u043B\\u044E\\u0447\",\"en\":\"Turnkey apartment renovation\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-apartment", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010202"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0578\\u057D\\u0574\\u0565\\u057F\\u056B\\u056F \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u041A\\u043E\\u0441\\u043C\\u0435\\u0442\\u0438\\u0447\\u0435\\u0441\\u043A\\u0438\\u0439 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442\",\"en\":\"Cosmetic renovation\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-cosmetic", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010203"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u057E\\u0561\\u0572\\u0578\\u0582\\u0574 \\u0587 \\u056E\\u0565\\u0583\\u0578\\u0582\\u0574\",\"ru\":\"\\u0428\\u0442\\u0443\\u043A\\u0430\\u0442\\u0443\\u0440\\u043A\\u0430 \\u0438 \\u0448\\u043F\\u0430\\u043A\\u043B\\u0451\\u0432\\u043A\\u0430\",\"en\":\"Plastering and skim coating\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-plastering", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010204"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0546\\u0565\\u0580\\u056F\\u0561\\u0580\\u0561\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u041C\\u0430\\u043B\\u044F\\u0440\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Painting\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-painting", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010205"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057D\\u057F\\u0561\\u057C\\u0561\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u043E\\u043A\\u043B\\u0435\\u0439\\u043A\\u0430 \\u043E\\u0431\\u043E\\u0435\\u0432\",\"en\":\"Wallpapering\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-wallpaper", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010206"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u0561\\u056C\\u056B\\u056F\\u0561\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u043A\\u043B\\u0430\\u0434\\u043A\\u0430 \\u043F\\u043B\\u0438\\u0442\\u043A\\u0438\",\"en\":\"Tiling\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-tiling", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010207"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0561\\u057F\\u0561\\u056F\\u056B \\u056E\\u0561\\u056E\\u056F (\\u056C\\u0561\\u0574\\u056B\\u0576\\u0561\\u057F, \\u0574\\u0561\\u0576\\u0580\\u0561\\u0570\\u0561\\u057F\\u0561\\u056F)\",\"ru\":\"\\u041D\\u0430\\u043F\\u043E\\u043B\\u044C\\u043D\\u044B\\u0435 \\u043F\\u043E\\u043A\\u0440\\u044B\\u0442\\u0438\\u044F (\\u043B\\u0430\\u043C\\u0438\\u043D\\u0430\\u0442, \\u043F\\u0430\\u0440\\u043A\\u0435\\u0442)\",\"en\":\"Flooring (laminate, parquet)\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-flooring", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010208"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0561\\u057F\\u0561\\u056F\\u056B \\u0570\\u0561\\u0580\\u0569\\u0565\\u0581\\u0578\\u0582\\u0574 (\\u057D\\u057F\\u0575\\u0561\\u056A\\u056F\\u0561)\",\"ru\":\"\\u0421\\u0442\\u044F\\u0436\\u043A\\u0430 \\u043F\\u043E\\u043B\\u0430\",\"en\":\"Floor screed\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-screed", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010209"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u057C\\u0561\\u057D\\u057F\\u0561\\u0572\\u0576\\u0565\\u0580 (\\u0571\\u0563\\u057E\\u0578\\u0572, \\u0563\\u056B\\u057A\\u057D\\u0561\\u057D\\u057F\\u057E\\u0561\\u0580\\u0561\\u0569\\u0572\\u0569\\u0565)\",\"ru\":\"\\u041F\\u043E\\u0442\\u043E\\u043B\\u043A\\u0438 (\\u043D\\u0430\\u0442\\u044F\\u0436\\u043D\\u044B\\u0435, \\u0438\\u0437 \\u0433\\u0438\\u043F\\u0441\\u043E\\u043A\\u0430\\u0440\\u0442\\u043E\\u043D\\u0430)\",\"en\":\"Ceilings (stretch, drywall)\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-ceilings", 9, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010210"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u056B\\u057A\\u057D\\u0561\\u057D\\u057F\\u057E\\u0561\\u0580\\u0561\\u0569\\u0572\\u0569\\u0565 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0420\\u0430\\u0431\\u043E\\u0442\\u044B \\u0441 \\u0433\\u0438\\u043F\\u0441\\u043E\\u043A\\u0430\\u0440\\u0442\\u043E\\u043D\\u043E\\u043C\",\"en\":\"Drywall\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-drywall", 10, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010211"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053C\\u0578\\u0563\\u0561\\u057D\\u0565\\u0576\\u0575\\u0561\\u056F\\u056B \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u0432\\u0430\\u043D\\u043D\\u043E\\u0439 \\u043A\\u043E\\u043C\\u043D\\u0430\\u0442\\u044B\",\"en\":\"Bathroom renovation\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-bathroom", 11, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010212"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053D\\u0578\\u0570\\u0561\\u0576\\u0578\\u0581\\u056B \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u043A\\u0443\\u0445\\u043D\\u0438\",\"en\":\"Kitchen renovation\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-kitchen", 12, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010213"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0534\\u0565\\u056F\\u0578\\u0580\\u0561\\u057F\\u056B\\u057E \\u056E\\u0565\\u0583 \\u0587 \\u057A\\u0561\\u057F\\u0565\\u0580\\u056B \\u0570\\u0561\\u0580\\u0564\\u0561\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0414\\u0435\\u043A\\u043E\\u0440\\u0430\\u0442\\u0438\\u0432\\u043D\\u0430\\u044F \\u0448\\u0442\\u0443\\u043A\\u0430\\u0442\\u0443\\u0440\\u043A\\u0430 \\u0438 \\u043E\\u0442\\u0434\\u0435\\u043B\\u043A\\u0430 \\u0441\\u0442\\u0435\\u043D\",\"en\":\"Decorative plaster and wall finishes\"}", new Guid("019a0000-0000-7000-8000-000000000102"), "renovation-decorative", 13, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010301"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u0561\\u0576\\u057F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\\u0575\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0441\\u0430\\u043D\\u0442\\u0435\\u0445\\u043D\\u0438\\u043A\\u0438\",\"en\":\"Fixture installation\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-fixtures", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010302"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0561\\u0563\\u056B\\u056E \\u0587 \\u056F\\u0578\\u0575\\u0578\\u0582\\u0572\\u056B\",\"ru\":\"\\u0412\\u043E\\u0434\\u043E\\u043F\\u0440\\u043E\\u0432\\u043E\\u0434 \\u0438 \\u043A\\u0430\\u043D\\u0430\\u043B\\u0438\\u0437\\u0430\\u0446\\u0438\\u044F\",\"en\":\"Water and sewer pipes\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-pipes", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010303"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u0580\\u057F\\u0561\\u0570\\u0578\\u057D\\u0584\\u056B \\u057E\\u0565\\u0580\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0440\\u0430\\u043D\\u0435\\u043D\\u0438\\u0435 \\u043F\\u0440\\u043E\\u0442\\u0435\\u0447\\u0435\\u043A\",\"en\":\"Leak repair\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-leaks", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010304"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0578\\u0575\\u0578\\u0582\\u0572\\u0578\\u0582 \\u056D\\u0581\\u0561\\u0576\\u0574\\u0561\\u0576 \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u0440\\u043E\\u0447\\u0438\\u0441\\u0442\\u043A\\u0430 \\u043A\\u0430\\u043D\\u0430\\u043B\\u0438\\u0437\\u0430\\u0446\\u0438\\u0438\",\"en\":\"Drain unclogging\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-drain-cleaning", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010305"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0561\\u057F\\u0561\\u0584\\u0561\\u0581\\u0578\\u0582\\u0581\\u056B\\u0579\\u0576\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0432\\u043E\\u0434\\u043E\\u043D\\u0430\\u0433\\u0440\\u0435\\u0432\\u0430\\u0442\\u0435\\u043B\\u0435\\u0439\",\"en\":\"Water heater installation\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-water-heaters", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010306"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0578\\u0574\\u057A\\u0565\\u0580 \\u0587 \\u057B\\u0580\\u056B \\u0586\\u056B\\u056C\\u057F\\u0580\\u0565\\u0580\",\"ru\":\"\\u041D\\u0430\\u0441\\u043E\\u0441\\u044B \\u0438 \\u0444\\u0438\\u043B\\u044C\\u0442\\u0440\\u044B \\u0434\\u043B\\u044F \\u0432\\u043E\\u0434\\u044B\",\"en\":\"Pumps and water filters\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-pumps-filters", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010307"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0561\\u0579\\u0561\\u0583\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0441\\u0447\\u0451\\u0442\\u0447\\u0438\\u043A\\u043E\\u0432 \\u0432\\u043E\\u0434\\u044B\",\"en\":\"Water meter installation\"}", new Guid("019a0000-0000-7000-8000-000000000103"), "plumbing-meters", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010401"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u0561\\u0566\\u056B \\u056F\\u0561\\u0569\\u057D\\u0561\\u0576\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574 \\u0587 \\u057D\\u057A\\u0561\\u057D\\u0561\\u0580\\u056F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0438 \\u043E\\u0431\\u0441\\u043B\\u0443\\u0436\\u0438\\u0432\\u0430\\u043D\\u0438\\u0435 \\u0433\\u0430\\u0437\\u043E\\u0432\\u044B\\u0445 \\u043A\\u043E\\u0442\\u043B\\u043E\\u0432\",\"en\":\"Gas boiler installation and service\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-boilers", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010402"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054C\\u0561\\u0564\\u056B\\u0561\\u057F\\u0578\\u0580\\u0576\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0440\\u0430\\u0434\\u0438\\u0430\\u0442\\u043E\\u0440\\u043E\\u0432\",\"en\":\"Radiator installation\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-radiators", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010403"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0561\\u0584 \\u0570\\u0561\\u057F\\u0561\\u056F\",\"ru\":\"\\u0422\\u0451\\u043F\\u043B\\u044B\\u0439 \\u043F\\u043E\\u043B\",\"en\":\"Underfloor heating\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-underfloor", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010404"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0565\\u057C\\u0578\\u0582\\u0581\\u0574\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u056B \\u0574\\u0578\\u0576\\u057F\\u0561\\u056A\",\"ru\":\"\\u041C\\u043E\\u043D\\u0442\\u0430\\u0436 \\u0441\\u0438\\u0441\\u0442\\u0435\\u043C\\u044B \\u043E\\u0442\\u043E\\u043F\\u043B\\u0435\\u043D\\u0438\\u044F\",\"en\":\"Heating system installation\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-systems", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010405"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0546\\u0565\\u0580\\u0584\\u056B\\u0576 \\u0563\\u0561\\u0566\\u0561\\u057F\\u0561\\u0580\",\"ru\":\"\\u0412\\u043D\\u0443\\u0442\\u0440\\u0435\\u043D\\u043D\\u0438\\u0439 \\u0433\\u0430\\u0437\\u043E\\u043F\\u0440\\u043E\\u0432\\u043E\\u0434\",\"en\":\"Indoor gas piping\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-gas-piping", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010406"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0532\\u0578\\u0582\\u056D\\u0561\\u0580\\u056B\\u0576\\u0565\\u0580 \\u0587 \\u057E\\u0561\\u057C\\u0561\\u0580\\u0561\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u041A\\u0430\\u043C\\u0438\\u043D\\u044B \\u0438 \\u043F\\u0435\\u0447\\u0438\",\"en\":\"Fireplaces and stoves\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-fireplaces", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010407"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0565\\u0580\\u0574\\u0561\\u0575\\u056B\\u0576 \\u057A\\u0578\\u0574\\u057A\\u0565\\u0580\",\"ru\":\"\\u0422\\u0435\\u043F\\u043B\\u043E\\u0432\\u044B\\u0435 \\u043D\\u0430\\u0441\\u043E\\u0441\\u044B\",\"en\":\"Heat pumps\"}", new Guid("019a0000-0000-7000-8000-000000000104"), "heating-heat-pumps", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010501"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0537\\u056C\\u0565\\u056F\\u057F\\u0580\\u0561\\u056C\\u0561\\u0580\\u0565\\u0580\\u056B \\u0561\\u0576\\u0581\\u056F\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u042D\\u043B\\u0435\\u043A\\u0442\\u0440\\u043E\\u043F\\u0440\\u043E\\u0432\\u043E\\u0434\\u043A\\u0430\",\"en\":\"Wiring\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-wiring", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010502"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0561\\u056F\\u0576\\u0565\\u0580 \\u0587 \\u0561\\u0576\\u057B\\u0561\\u057F\\u056B\\u0579\\u0576\\u0565\\u0580\",\"ru\":\"\\u0420\\u043E\\u0437\\u0435\\u0442\\u043A\\u0438 \\u0438 \\u0432\\u044B\\u043A\\u043B\\u044E\\u0447\\u0430\\u0442\\u0435\\u043B\\u0438\",\"en\":\"Sockets and switches\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-sockets", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010503"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u057E\\u0578\\u0580\\u0578\\u0582\\u0569\\u0575\\u0561\\u0576 \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u043E\\u043D\\u0442\\u0430\\u0436 \\u043E\\u0441\\u0432\\u0435\\u0449\\u0435\\u043D\\u0438\\u044F\",\"en\":\"Lighting installation\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-lighting", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010504"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0537\\u056C\\u0565\\u056F\\u057F\\u0580\\u0561\\u056F\\u0561\\u0576 \\u057E\\u0561\\u0570\\u0561\\u0576\\u0561\\u056F\\u0576\\u0565\\u0580\",\"ru\":\"\\u042D\\u043B\\u0435\\u043A\\u0442\\u0440\\u043E\\u0449\\u0438\\u0442\\u044B\",\"en\":\"Electrical panels\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-panels", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010505"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0537\\u056C\\u0565\\u056F\\u057F\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0561\\u0576\\u057D\\u0561\\u0580\\u0584\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576\\u0576\\u0565\\u0580\\u056B \\u057E\\u0565\\u0580\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0440\\u0430\\u043D\\u0435\\u043D\\u0438\\u0435 \\u043D\\u0435\\u0438\\u0441\\u043F\\u0440\\u0430\\u0432\\u043D\\u043E\\u0441\\u0442\\u0435\\u0439 \\u044D\\u043B\\u0435\\u043A\\u0442\\u0440\\u0438\\u043A\\u0438\",\"en\":\"Electrical fault repair\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-repair", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010506"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u0580\\u0587\\u0561\\u0575\\u056B\\u0576 \\u057E\\u0561\\u0570\\u0561\\u0576\\u0561\\u056F\\u0576\\u0565\\u0580\",\"ru\":\"\\u0421\\u043E\\u043B\\u043D\\u0435\\u0447\\u043D\\u044B\\u0435 \\u043F\\u0430\\u043D\\u0435\\u043B\\u0438\",\"en\":\"Solar panels\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-solar", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010507"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u0565\\u0576\\u0565\\u0580\\u0561\\u057F\\u0578\\u0580\\u0576\\u0565\\u0580 \\u0587 \\u0561\\u0576\\u056D\\u0561\\u0583\\u0561\\u0576 \\u057D\\u0576\\u0578\\u0582\\u0581\\u0574\\u0561\\u0576 \\u057D\\u0561\\u0580\\u0584\\u0565\\u0580\",\"ru\":\"\\u0413\\u0435\\u043D\\u0435\\u0440\\u0430\\u0442\\u043E\\u0440\\u044B \\u0438 \\u0418\\u0411\\u041F\",\"en\":\"Generators and UPS\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-generators", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010508"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0537\\u056C\\u0565\\u056F\\u057F\\u0580\\u0561\\u0574\\u0565\\u0584\\u0565\\u0576\\u0561\\u0576\\u0565\\u0580\\u056B \\u056C\\u056B\\u0581\\u0584\\u0561\\u057E\\u0578\\u0580\\u0574\\u0561\\u0576 \\u056F\\u0561\\u0575\\u0561\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u0417\\u0430\\u0440\\u044F\\u0434\\u043D\\u044B\\u0435 \\u0441\\u0442\\u0430\\u043D\\u0446\\u0438\\u0438 \\u0434\\u043B\\u044F \\u044D\\u043B\\u0435\\u043A\\u0442\\u0440\\u043E\\u043C\\u043E\\u0431\\u0438\\u043B\\u0435\\u0439\",\"en\":\"EV chargers\"}", new Guid("019a0000-0000-7000-8000-000000000105"), "electrical-ev-chargers", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010601"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0554\\u0561\\u0580\\u0565 \\u0565\\u0580\\u0565\\u057D\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574 (\\u057F\\u0578\\u0582\\u0586, \\u0562\\u0561\\u0566\\u0561\\u056C\\u057F)\",\"ru\":\"\\u041E\\u0431\\u043B\\u0438\\u0446\\u043E\\u0432\\u043A\\u0430 \\u043A\\u0430\\u043C\\u043D\\u0435\\u043C (\\u0442\\u0443\\u0444, \\u0431\\u0430\\u0437\\u0430\\u043B\\u044C\\u0442)\",\"en\":\"Stone cladding (tuff, basalt)\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-stone", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010602"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u056B \\u057D\\u057E\\u0561\\u0572\\u0578\\u0582\\u0574 \\u0587 \\u0576\\u0565\\u0580\\u056F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0428\\u0442\\u0443\\u043A\\u0430\\u0442\\u0443\\u0440\\u043A\\u0430 \\u0438 \\u043F\\u043E\\u043A\\u0440\\u0430\\u0441\\u043A\\u0430 \\u0444\\u0430\\u0441\\u0430\\u0434\\u0430\",\"en\":\"Facade plastering and painting\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-plaster", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010603"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0555\\u0564\\u0561\\u0583\\u0578\\u056D\\u057E\\u0578\\u0572 \\u0586\\u0561\\u057D\\u0561\\u0564\\u0576\\u0565\\u0580\",\"ru\":\"\\u0412\\u0435\\u043D\\u0442\\u0438\\u043B\\u0438\\u0440\\u0443\\u0435\\u043C\\u044B\\u0435 \\u0444\\u0430\\u0441\\u0430\\u0434\\u044B\",\"en\":\"Ventilated facades\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-ventilated", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010604"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0578\\u0574\\u057A\\u0578\\u0566\\u056B\\u057F\\u0561\\u0575\\u056B\\u0576 \\u0587 \\u057D\\u0561\\u0575\\u0564\\u056B\\u0576\\u0563\\u0561\\u0575\\u056B\\u0576 \\u0565\\u0580\\u0565\\u057D\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u043B\\u0438\\u0446\\u043E\\u0432\\u043A\\u0430 \\u043A\\u043E\\u043C\\u043F\\u043E\\u0437\\u0438\\u0442\\u043E\\u043C \\u0438 \\u0441\\u0430\\u0439\\u0434\\u0438\\u043D\\u0433\\u043E\\u043C\",\"en\":\"Composite panels and siding\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-composite", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010605"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u056B \\u057B\\u0565\\u0580\\u0574\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0442\\u0435\\u043F\\u043B\\u0435\\u043D\\u0438\\u0435 \\u0444\\u0430\\u0441\\u0430\\u0434\\u0430\",\"en\":\"Facade insulation\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-insulation", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010606"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u056B \\u057E\\u0565\\u0580\\u0561\\u056F\\u0561\\u0576\\u0563\\u0576\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u0441\\u0442\\u0430\\u0432\\u0440\\u0430\\u0446\\u0438\\u044F \\u0444\\u0430\\u0441\\u0430\\u0434\\u0430\",\"en\":\"Facade restoration\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-restoration", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010607"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u0580\\u0564\\u0575\\u0578\\u0582\\u0576\\u0561\\u0562\\u0565\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0561\\u056C\\u057A\\u056B\\u0576\\u056B\\u0566\\u0574\",\"ru\":\"\\u041F\\u0440\\u043E\\u043C\\u044B\\u0448\\u043B\\u0435\\u043D\\u043D\\u044B\\u0439 \\u0430\\u043B\\u044C\\u043F\\u0438\\u043D\\u0438\\u0437\\u043C\",\"en\":\"Rope access work\"}", new Guid("019a0000-0000-7000-8000-000000000106"), "facade-rope-access", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010701"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0561\\u0576\\u0578\\u0576\\u0561\\u057E\\u0578\\u0580 \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u0433\\u0443\\u043B\\u044F\\u0440\\u043D\\u0430\\u044F \\u0443\\u0431\\u043E\\u0440\\u043A\\u0430\",\"en\":\"Regular cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-regular", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010702"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u056C\\u056D\\u0561\\u057E\\u0578\\u0580 \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0413\\u0435\\u043D\\u0435\\u0440\\u0430\\u043B\\u044C\\u043D\\u0430\\u044F \\u0443\\u0431\\u043E\\u0440\\u043A\\u0430\",\"en\":\"Deep cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-deep", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010703"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\\u056B\\u0581 \\u0570\\u0565\\u057F\\u0578 \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0431\\u043E\\u0440\\u043A\\u0430 \\u043F\\u043E\\u0441\\u043B\\u0435 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442\\u0430\",\"en\":\"Post-renovation cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-after-renovation", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010704"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580\\u056B \\u056C\\u057E\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u043E\\u0439\\u043A\\u0430 \\u043E\\u043A\\u043E\\u043D\",\"en\":\"Window cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-windows", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010705"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0553\\u0561\\u0583\\u0578\\u0582\\u056F \\u056F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584\\u056B \\u0584\\u056B\\u0574\\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0425\\u0438\\u043C\\u0447\\u0438\\u0441\\u0442\\u043A\\u0430 \\u043C\\u044F\\u0433\\u043A\\u043E\\u0439 \\u043C\\u0435\\u0431\\u0435\\u043B\\u0438\",\"en\":\"Upholstery cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-upholstery", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010706"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u0578\\u0580\\u0563\\u0565\\u0580\\u056B \\u056C\\u057E\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0427\\u0438\\u0441\\u0442\\u043A\\u0430 \\u043A\\u043E\\u0432\\u0440\\u043E\\u0432\",\"en\":\"Carpet cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-carpets", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010707"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u0580\\u0561\\u057D\\u0565\\u0576\\u0575\\u0561\\u056F\\u0576\\u0565\\u0580\\u056B \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0431\\u043E\\u0440\\u043A\\u0430 \\u043E\\u0444\\u0438\\u0441\\u043E\\u0432\",\"en\":\"Office cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-offices", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010708"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0565\\u0572\\u0561\\u0583\\u0578\\u056D\\u0578\\u0582\\u0569\\u0575\\u0561\\u0576 \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0431\\u043E\\u0440\\u043A\\u0430 \\u043F\\u0440\\u0438 \\u043F\\u0435\\u0440\\u0435\\u0435\\u0437\\u0434\\u0435\",\"en\":\"Move-in and move-out cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-move", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010709"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u0576\\u0565\\u0580\\u056B \\u0587 \\u057E\\u056B\\u057F\\u0580\\u0561\\u056A\\u0576\\u0565\\u0580\\u056B \\u056C\\u057E\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u043E\\u0439\\u043A\\u0430 \\u0444\\u0430\\u0441\\u0430\\u0434\\u043E\\u0432 \\u0438 \\u0432\\u0438\\u0442\\u0440\\u0430\\u0436\\u0435\\u0439\",\"en\":\"Facade and glass cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-facades", 9, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010710"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u056D\\u057F\\u0561\\u0570\\u0561\\u0576\\u0578\\u0582\\u0574 \\u0587 \\u057E\\u0576\\u0561\\u057D\\u0561\\u057F\\u0578\\u0582\\u0576\\u0565\\u0580\\u056B \\u0578\\u0579\\u0576\\u0579\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0414\\u0435\\u0437\\u0438\\u043D\\u0444\\u0435\\u043A\\u0446\\u0438\\u044F \\u0438 \\u0434\\u0435\\u0437\\u0438\\u043D\\u0441\\u0435\\u043A\\u0446\\u0438\\u044F\",\"en\":\"Disinfection and pest control\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-disinfection", 10, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010711"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053D\\u0578\\u0570\\u0561\\u0576\\u0578\\u0581\\u056B \\u0587 \\u056F\\u0565\\u0576\\u0581\\u0561\\u0572\\u0561\\u0575\\u056B\\u0576 \\u057F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\\u0575\\u056B \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0427\\u0438\\u0441\\u0442\\u043A\\u0430 \\u043A\\u0443\\u0445\\u043D\\u0438 \\u0438 \\u0431\\u044B\\u0442\\u043E\\u0432\\u043E\\u0439 \\u0442\\u0435\\u0445\\u043D\\u0438\\u043A\\u0438\",\"en\":\"Kitchen and appliance cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-kitchens", 11, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010712"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0532\\u0561\\u056F\\u056B \\u0587 \\u057F\\u0561\\u0580\\u0561\\u056E\\u0584\\u056B \\u0574\\u0561\\u0584\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0431\\u043E\\u0440\\u043A\\u0430 \\u0434\\u0432\\u043E\\u0440\\u0430 \\u0438 \\u0442\\u0435\\u0440\\u0440\\u0438\\u0442\\u043E\\u0440\\u0438\\u0438\",\"en\":\"Yard and outdoor cleaning\"}", new Guid("019a0000-0000-7000-8000-000000000107"), "cleaning-yard", 12, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010801"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0546\\u0578\\u0580 \\u057F\\u0561\\u0576\\u056B\\u0584\\u056B \\u056F\\u0561\\u057C\\u0578\\u0582\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u043E\\u043D\\u0442\\u0430\\u0436 \\u043D\\u043E\\u0432\\u043E\\u0439 \\u043A\\u0440\\u043E\\u0432\\u043B\\u0438\",\"en\":\"New roof installation\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-new", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010802"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0561\\u0576\\u056B\\u0584\\u056B \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u043A\\u0440\\u043E\\u0432\\u043B\\u0438\",\"en\":\"Roof repair\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-repair", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010803"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0580\\u0578\\u0586\\u0576\\u0561\\u057D\\u057F\\u056B\\u056C \\u0587 \\u0574\\u0565\\u057F\\u0561\\u0572\\u0561\\u057D\\u0561\\u056C\\u056B\\u056F\",\"ru\":\"\\u041F\\u0440\\u043E\\u0444\\u043D\\u0430\\u0441\\u0442\\u0438\\u043B \\u0438 \\u043C\\u0435\\u0442\\u0430\\u043B\\u043B\\u043E\\u0447\\u0435\\u0440\\u0435\\u043F\\u0438\\u0446\\u0430\",\"en\":\"Metal sheets and metal tiles\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-metal", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010804"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0561\\u0580\\u0569 \\u057F\\u0561\\u0576\\u056B\\u0584\\u056B \\u057B\\u0580\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0413\\u0438\\u0434\\u0440\\u043E\\u0438\\u0437\\u043E\\u043B\\u044F\\u0446\\u0438\\u044F \\u043F\\u043B\\u043E\\u0441\\u043A\\u043E\\u0439 \\u043A\\u0440\\u043E\\u0432\\u043B\\u0438\",\"en\":\"Flat roof waterproofing\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-flat", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010805"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0561\\u0570\\u0565\\u057C\\u0561\\u0581\\u0574\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u0412\\u043E\\u0434\\u043E\\u0441\\u0442\\u043E\\u043A\\u0438\",\"en\":\"Gutters and downpipes\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-gutters", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010806"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0541\\u0565\\u0572\\u0576\\u0561\\u0570\\u0561\\u0580\\u056F\\u056B \\u056F\\u0561\\u057C\\u0578\\u0582\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041C\\u0430\\u043D\\u0441\\u0430\\u0440\\u0434\\u044B\",\"en\":\"Attic and loft conversion\"}", new Guid("019a0000-0000-7000-8000-000000000108"), "roofing-attic", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010901"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0544\\u0565\\u057F\\u0561\\u0572\\u0561\\u057A\\u056C\\u0561\\u057D\\u057F\\u0565 \\u057A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u041C\\u0435\\u0442\\u0430\\u043B\\u043B\\u043E\\u043F\\u043B\\u0430\\u0441\\u0442\\u0438\\u043A\\u043E\\u0432\\u044B\\u0435 \\u043E\\u043A\\u043D\\u0430\",\"en\":\"uPVC windows\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "windows-pvc", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010902"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u056C\\u0575\\u0578\\u0582\\u0574\\u056B\\u0576\\u0565 \\u057A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580 \\u0587 \\u057E\\u056B\\u057F\\u0580\\u0561\\u056A\\u0576\\u0565\\u0580\",\"ru\":\"\\u0410\\u043B\\u044E\\u043C\\u0438\\u043D\\u0438\\u0435\\u0432\\u044B\\u0435 \\u043E\\u043A\\u043D\\u0430 \\u0438 \\u0432\\u0438\\u0442\\u0440\\u0430\\u0436\\u0438\",\"en\":\"Aluminium windows and glazing\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "windows-aluminium", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010903"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0577\\u0563\\u0561\\u0574\\u0562\\u0576\\u0565\\u0580\\u056B \\u0561\\u057A\\u0561\\u056F\\u0565\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0441\\u0442\\u0435\\u043A\\u043B\\u0435\\u043D\\u0438\\u0435 \\u0431\\u0430\\u043B\\u043A\\u043E\\u043D\\u043E\\u0432\",\"en\":\"Balcony glazing\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "windows-balcony", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010904"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0546\\u0565\\u0580\\u0584\\u056B\\u0576 \\u0564\\u057C\\u0576\\u0565\\u0580\",\"ru\":\"\\u041C\\u0435\\u0436\\u043A\\u043E\\u043C\\u043D\\u0430\\u0442\\u043D\\u044B\\u0435 \\u0434\\u0432\\u0435\\u0440\\u0438\",\"en\":\"Interior doors\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "doors-interior", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010905"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0544\\u0578\\u0582\\u057F\\u0584\\u056B \\u0587 \\u0574\\u0565\\u057F\\u0561\\u0572\\u0561\\u056F\\u0561\\u0576 \\u0564\\u057C\\u0576\\u0565\\u0580\",\"ru\":\"\\u0412\\u0445\\u043E\\u0434\\u043D\\u044B\\u0435 \\u0438 \\u043C\\u0435\\u0442\\u0430\\u043B\\u043B\\u0438\\u0447\\u0435\\u0441\\u043A\\u0438\\u0435 \\u0434\\u0432\\u0435\\u0440\\u0438\",\"en\":\"Entrance and steel doors\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "doors-entrance", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010906"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580\\u056B \\u0587 \\u0564\\u057C\\u0576\\u0565\\u0580\\u056B \\u056F\\u0561\\u0580\\u0563\\u0561\\u057E\\u0578\\u0580\\u0578\\u0582\\u0574 \\u0578\\u0582 \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u0433\\u0443\\u043B\\u0438\\u0440\\u043E\\u0432\\u043A\\u0430 \\u0438 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442 \\u043E\\u043A\\u043E\\u043D \\u0438 \\u0434\\u0432\\u0435\\u0440\\u0435\\u0439\",\"en\":\"Window and door repair\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "windows-repair", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010907"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0547\\u0565\\u0580\\u057F\\u0561\\u057E\\u0561\\u0580\\u0561\\u0563\\u0578\\u0582\\u0575\\u0580\\u0576\\u0565\\u0580 \\u0587 \\u057C\\u0578\\u056C\\u0565\\u057F\\u0576\\u0565\\u0580\",\"ru\":\"\\u0416\\u0430\\u043B\\u044E\\u0437\\u0438 \\u0438 \\u0440\\u043E\\u043B\\u044C\\u0441\\u0442\\u0430\\u0432\\u043D\\u0438\",\"en\":\"Blinds and roller shutters\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "windows-blinds", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000010908"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u057E\\u057F\\u0578\\u057F\\u0576\\u0561\\u056F\\u056B \\u0564\\u0561\\u0580\\u057A\\u0561\\u057D\\u0576\\u0565\\u0580\",\"ru\":\"\\u0413\\u0430\\u0440\\u0430\\u0436\\u043D\\u044B\\u0435 \\u0432\\u043E\\u0440\\u043E\\u0442\\u0430\",\"en\":\"Garage doors\"}", new Guid("019a0000-0000-7000-8000-000000000109"), "doors-garage", 8, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011001"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0555\\u0564\\u0578\\u0580\\u0561\\u056F\\u056B\\u0579\\u0576\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u043A\\u043E\\u043D\\u0434\\u0438\\u0446\\u0438\\u043E\\u043D\\u0435\\u0440\\u043E\\u0432\",\"en\":\"Air conditioner installation\"}", new Guid("019a0000-0000-7000-8000-000000000110"), "hvac-ac-installation", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011002"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0555\\u0564\\u0578\\u0580\\u0561\\u056F\\u056B\\u0579\\u0576\\u0565\\u0580\\u056B \\u057D\\u057A\\u0561\\u057D\\u0561\\u0580\\u056F\\u0578\\u0582\\u0574 \\u0587 \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u0441\\u043B\\u0443\\u0436\\u0438\\u0432\\u0430\\u043D\\u0438\\u0435 \\u0438 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442 \\u043A\\u043E\\u043D\\u0434\\u0438\\u0446\\u0438\\u043E\\u043D\\u0435\\u0440\\u043E\\u0432\",\"en\":\"Air conditioner service and repair\"}", new Guid("019a0000-0000-7000-8000-000000000110"), "hvac-ac-service", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011003"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0555\\u0564\\u0561\\u0583\\u0578\\u056D\\u0578\\u0582\\u0569\\u0575\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u0421\\u0438\\u0441\\u0442\\u0435\\u043C\\u044B \\u0432\\u0435\\u043D\\u0442\\u0438\\u043B\\u044F\\u0446\\u0438\\u0438\",\"en\":\"Ventilation systems\"}", new Guid("019a0000-0000-7000-8000-000000000110"), "hvac-ventilation", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011004"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053D\\u0578\\u0570\\u0561\\u0576\\u0578\\u0581\\u0561\\u0575\\u056B\\u0576 \\u0585\\u0564\\u0561\\u0584\\u0561\\u0580\\u0577\\u0576\\u0565\\u0580\",\"ru\":\"\\u041A\\u0443\\u0445\\u043E\\u043D\\u043D\\u044B\\u0435 \\u0432\\u044B\\u0442\\u044F\\u0436\\u043A\\u0438\",\"en\":\"Kitchen hoods\"}", new Guid("019a0000-0000-7000-8000-000000000110"), "hvac-hoods", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011005"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054C\\u0565\\u056F\\u0578\\u0582\\u057A\\u0565\\u0580\\u0561\\u057F\\u0578\\u0580\\u0576\\u0565\\u0580\",\"ru\":\"\\u0420\\u0435\\u043A\\u0443\\u043F\\u0435\\u0440\\u0430\\u0442\\u043E\\u0440\\u044B\",\"en\":\"Heat recovery ventilation\"}", new Guid("019a0000-0000-7000-8000-000000000110"), "hvac-heat-recovery", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011101"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0565\\u0580\\u056B \\u0587 \\u0570\\u0561\\u057F\\u0561\\u056F\\u056B \\u057B\\u0565\\u0580\\u0574\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0442\\u0435\\u043F\\u043B\\u0435\\u043D\\u0438\\u0435 \\u0441\\u0442\\u0435\\u043D \\u0438 \\u043F\\u043E\\u043B\\u0430\",\"en\":\"Wall and floor insulation\"}", new Guid("019a0000-0000-7000-8000-000000000111"), "insulation-thermal", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011102"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0561\\u0576\\u056B\\u0584\\u056B \\u057B\\u0565\\u0580\\u0574\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0442\\u0435\\u043F\\u043B\\u0435\\u043D\\u0438\\u0435 \\u043A\\u0440\\u043E\\u0432\\u043B\\u0438\",\"en\":\"Roof insulation\"}", new Guid("019a0000-0000-7000-8000-000000000111"), "insulation-roof", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011103"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574 (\\u0576\\u056F\\u0578\\u0582\\u0572, \\u056C\\u0578\\u0563\\u0561\\u057D\\u0565\\u0576\\u0575\\u0561\\u056F)\",\"ru\":\"\\u0413\\u0438\\u0434\\u0440\\u043E\\u0438\\u0437\\u043E\\u043B\\u044F\\u0446\\u0438\\u044F (\\u043F\\u043E\\u0434\\u0432\\u0430\\u043B, \\u0432\\u0430\\u043D\\u043D\\u0430\\u044F)\",\"en\":\"Waterproofing (basement, bathroom)\"}", new Guid("019a0000-0000-7000-8000-000000000111"), "insulation-waterproofing", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011104"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0541\\u0561\\u0575\\u0576\\u0561\\u0574\\u0565\\u056F\\u0578\\u0582\\u057D\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0417\\u0432\\u0443\\u043A\\u043E\\u0438\\u0437\\u043E\\u043B\\u044F\\u0446\\u0438\\u044F\",\"en\":\"Soundproofing\"}", new Guid("019a0000-0000-7000-8000-000000000111"), "insulation-sound", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011105"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0578\\u056C\\u056B\\u0578\\u0582\\u0580\\u0565\\u0569\\u0561\\u0576\\u0561\\u0575\\u056B\\u0576 \\u0583\\u0580\\u0583\\u0578\\u0582\\u0580\\u056B \\u0583\\u0579\\u0578\\u0582\\u0574\",\"ru\":\"\\u041D\\u0430\\u043F\\u044B\\u043B\\u0435\\u043D\\u0438\\u0435 \\u043F\\u0435\\u043D\\u043E\\u043F\\u043E\\u043B\\u0438\\u0443\\u0440\\u0435\\u0442\\u0430\\u043D\\u0430\",\"en\":\"Spray foam insulation\"}", new Guid("019a0000-0000-7000-8000-000000000111"), "insulation-spray-foam", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011201"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0543\\u0561\\u0572\\u0561\\u057E\\u0561\\u0576\\u0564\\u0561\\u056F\\u0576\\u0565\\u0580 \\u0587 \\u0562\\u0561\\u0566\\u0580\\u056B\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u041E\\u0433\\u0440\\u0430\\u0436\\u0434\\u0435\\u043D\\u0438\\u044F \\u0438 \\u043F\\u0435\\u0440\\u0438\\u043B\\u0430\",\"en\":\"Railings and balustrades\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-railings", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011202"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0534\\u0561\\u0580\\u057A\\u0561\\u057D\\u0576\\u0565\\u0580 \\u0587 \\u0581\\u0561\\u0576\\u056F\\u0561\\u057A\\u0561\\u057F\\u0565\\u0580\",\"ru\":\"\\u0412\\u043E\\u0440\\u043E\\u0442\\u0430 \\u0438 \\u0437\\u0430\\u0431\\u043E\\u0440\\u044B\",\"en\":\"Gates and fences\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-gates-fences", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011203"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0544\\u0565\\u057F\\u0561\\u0572\\u0561\\u056F\\u0561\\u0576 \\u056F\\u0578\\u0576\\u057D\\u057F\\u0580\\u0578\\u0582\\u056F\\u0581\\u056B\\u0561\\u0576\\u0565\\u0580\",\"ru\":\"\\u041C\\u0435\\u0442\\u0430\\u043B\\u043B\\u043E\\u043A\\u043E\\u043D\\u0441\\u0442\\u0440\\u0443\\u043A\\u0446\\u0438\\u0438\",\"en\":\"Steel structures\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-structures", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011204"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053E\\u0561\\u056E\\u056F\\u0565\\u0580 \\u0587 \\u0576\\u0561\\u057E\\u0565\\u057D\\u0576\\u0565\\u0580\",\"ru\":\"\\u041D\\u0430\\u0432\\u0435\\u0441\\u044B\",\"en\":\"Canopies and carports\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-canopies", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011205"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0535\\u057C\\u0561\\u056F\\u0581\\u0574\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0421\\u0432\\u0430\\u0440\\u043E\\u0447\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Welding\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-welding", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011206"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0580\\u057E\\u0565\\u057D\\u057F\\u0561\\u056F\\u0561\\u0576 \\u0564\\u0561\\u0580\\u0562\\u0576\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576\",\"ru\":\"\\u0425\\u0443\\u0434\\u043E\\u0436\\u0435\\u0441\\u0442\\u0432\\u0435\\u043D\\u043D\\u0430\\u044F \\u043A\\u043E\\u0432\\u043A\\u0430\",\"en\":\"Decorative forging\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-forging", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011207"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054A\\u0561\\u057F\\u0578\\u0582\\u0570\\u0561\\u0576\\u0576\\u0565\\u0580\\u056B \\u057E\\u0561\\u0576\\u0564\\u0561\\u056F\\u0561\\u0573\\u0561\\u0572\\u0565\\u0580\",\"ru\":\"\\u0420\\u0435\\u0448\\u0451\\u0442\\u043A\\u0438 \\u043D\\u0430 \\u043E\\u043A\\u043D\\u0430\",\"en\":\"Window grilles\"}", new Guid("019a0000-0000-7000-8000-000000000112"), "metalwork-window-grilles", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011301"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053D\\u0578\\u0570\\u0561\\u0576\\u0578\\u0581\\u0561\\u0575\\u056B\\u0576 \\u056F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584 \\u057A\\u0561\\u057F\\u057E\\u0565\\u0580\\u0578\\u057E\",\"ru\":\"\\u041A\\u0443\\u0445\\u043D\\u0438 \\u043D\\u0430 \\u0437\\u0430\\u043A\\u0430\\u0437\",\"en\":\"Custom kitchens\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-kitchens", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011302"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0536\\u0563\\u0565\\u057D\\u057F\\u0561\\u057A\\u0561\\u0570\\u0561\\u0580\\u0561\\u0576\\u0576\\u0565\\u0580 \\u0587 \\u0576\\u0565\\u0580\\u056F\\u0561\\u057C\\u0578\\u0582\\u0581\\u057E\\u0561\\u056E \\u056F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584\",\"ru\":\"\\u0428\\u043A\\u0430\\u0444\\u044B \\u0438 \\u0432\\u0441\\u0442\\u0440\\u043E\\u0435\\u043D\\u043D\\u0430\\u044F \\u043C\\u0435\\u0431\\u0435\\u043B\\u044C\",\"en\":\"Wardrobes and built-in furniture\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-wardrobes", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011303"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584\\u056B \\u0570\\u0561\\u057E\\u0561\\u0584\\u0578\\u0582\\u0574\",\"ru\":\"\\u0421\\u0431\\u043E\\u0440\\u043A\\u0430 \\u043C\\u0435\\u0431\\u0435\\u043B\\u0438\",\"en\":\"Furniture assembly\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-assembly", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011304"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0561\\u0570\\u0578\\u0582\\u0575\\u0584\\u056B \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574 \\u0587 \\u057E\\u0565\\u0580\\u0561\\u057A\\u0561\\u057D\\u057F\\u0561\\u057C\\u0561\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442 \\u0438 \\u043F\\u0435\\u0440\\u0435\\u0442\\u044F\\u0436\\u043A\\u0430 \\u043C\\u0435\\u0431\\u0435\\u043B\\u0438\",\"en\":\"Furniture repair and reupholstery\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-furniture-repair", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011305"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0553\\u0561\\u0575\\u057F\\u0565 \\u0561\\u057D\\u057F\\u056B\\u0573\\u0561\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u0414\\u0435\\u0440\\u0435\\u0432\\u044F\\u043D\\u043D\\u044B\\u0435 \\u043B\\u0435\\u0441\\u0442\\u043D\\u0438\\u0446\\u044B\",\"en\":\"Wooden stairs\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-wooden-stairs", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011306"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0553\\u0561\\u0575\\u057F\\u0565 \\u056F\\u0561\\u057C\\u0578\\u0582\\u0575\\u0581\\u0576\\u0565\\u0580 (\\u057F\\u0561\\u0572\\u0561\\u057E\\u0561\\u0580\\u0576\\u0565\\u0580, \\u057F\\u0565\\u057C\\u0561\\u057D\\u0576\\u0565\\u0580)\",\"ru\":\"\\u0414\\u0435\\u0440\\u0435\\u0432\\u044F\\u043D\\u043D\\u044B\\u0435 \\u043A\\u043E\\u043D\\u0441\\u0442\\u0440\\u0443\\u043A\\u0446\\u0438\\u0438 (\\u0431\\u0435\\u0441\\u0435\\u0434\\u043A\\u0438, \\u0442\\u0435\\u0440\\u0440\\u0430\\u0441\\u044B)\",\"en\":\"Wooden structures (gazebos, decks)\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-wooden-structures", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011307"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u0565\\u0572\\u0561\\u0576\\u056B \\u0574\\u0561\\u056F\\u0565\\u0580\\u0565\\u057D\\u0576\\u0565\\u0580 (\\u0584\\u0561\\u0580, \\u0583\\u0561\\u0575\\u057F)\",\"ru\":\"\\u0421\\u0442\\u043E\\u043B\\u0435\\u0448\\u043D\\u0438\\u0446\\u044B (\\u043A\\u0430\\u043C\\u0435\\u043D\\u044C, \\u0434\\u0435\\u0440\\u0435\\u0432\\u043E)\",\"en\":\"Countertops (stone, wood)\"}", new Guid("019a0000-0000-7000-8000-000000000113"), "carpentry-countertops", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011401"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0553\\u0578\\u0580\\u0574\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0417\\u0435\\u043C\\u043B\\u044F\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B \\u0438 \\u043A\\u043E\\u0442\\u043B\\u043E\\u0432\\u0430\\u043D\\u044B\",\"en\":\"Excavation\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-excavation", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011402"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0554\\u0561\\u0576\\u0564\\u0574\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u0414\\u0435\\u043C\\u043E\\u043D\\u0442\\u0430\\u0436\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Demolition\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-demolition", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011403"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0547\\u056B\\u0576\\u0561\\u0580\\u0561\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0561\\u0572\\u0562\\u056B \\u0564\\u0578\\u0582\\u0580\\u057D\\u0562\\u0565\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0412\\u044B\\u0432\\u043E\\u0437 \\u0441\\u0442\\u0440\\u043E\\u0438\\u0442\\u0435\\u043B\\u044C\\u043D\\u043E\\u0433\\u043E \\u043C\\u0443\\u0441\\u043E\\u0440\\u0430\",\"en\":\"Construction waste removal\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-waste-removal", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011404"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0532\\u0565\\u057F\\u0578\\u0576\\u056B \\u056F\\u057F\\u0580\\u0578\\u0582\\u0574 \\u0587 \\u0561\\u056C\\u0574\\u0561\\u057D\\u057F\\u0565 \\u0570\\u0578\\u0580\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u0437\\u043A\\u0430 \\u0431\\u0435\\u0442\\u043E\\u043D\\u0430 \\u0438 \\u0430\\u043B\\u043C\\u0430\\u0437\\u043D\\u043E\\u0435 \\u0431\\u0443\\u0440\\u0435\\u043D\\u0438\\u0435\",\"en\":\"Concrete cutting and core drilling\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-concrete-cutting", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011405"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0547\\u056B\\u0576\\u057F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\\u0575\\u056B \\u057E\\u0561\\u0580\\u0571\\u0578\\u0582\\u0575\\u0569\",\"ru\":\"\\u0410\\u0440\\u0435\\u043D\\u0434\\u0430 \\u0441\\u043F\\u0435\\u0446\\u0442\\u0435\\u0445\\u043D\\u0438\\u043A\\u0438\",\"en\":\"Construction equipment rental\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-equipment", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011406"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u0565\\u057A\\u057F\\u056B\\u056F\\u0576\\u0565\\u0580 \\u0587 \\u0564\\u0580\\u0565\\u0576\\u0561\\u056A\",\"ru\":\"\\u0421\\u0435\\u043F\\u0442\\u0438\\u043A\\u0438 \\u0438 \\u0434\\u0440\\u0435\\u043D\\u0430\\u0436\",\"en\":\"Septic tanks and drainage\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-septic", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011407"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054B\\u0580\\u0570\\u0578\\u0580\\u0576\\u0565\\u0580\\u056B \\u0570\\u0578\\u0580\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0411\\u0443\\u0440\\u0435\\u043D\\u0438\\u0435 \\u0441\\u043A\\u0432\\u0430\\u0436\\u0438\\u043D\",\"en\":\"Well drilling\"}", new Guid("019a0000-0000-7000-8000-000000000114"), "earthworks-wells", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011501"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053C\\u0561\\u0576\\u0564\\u0577\\u0561\\u0586\\u057F\\u0561\\u0575\\u056B\\u0576 \\u0564\\u056B\\u0566\\u0561\\u0575\\u0576\",\"ru\":\"\\u041B\\u0430\\u043D\\u0434\\u0448\\u0430\\u0444\\u0442\\u043D\\u044B\\u0439 \\u0434\\u0438\\u0437\\u0430\\u0439\\u043D\",\"en\":\"Landscape design\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-design", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011502"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u0561\\u056C\\u0561\\u0570\\u0561\\u057F\\u0561\\u056F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u043A\\u043B\\u0430\\u0434\\u043A\\u0430 \\u0442\\u0440\\u043E\\u0442\\u0443\\u0430\\u0440\\u043D\\u043E\\u0439 \\u043F\\u043B\\u0438\\u0442\\u043A\\u0438\",\"en\":\"Paving\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-paving", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011503"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054D\\u056B\\u0566\\u0561\\u0574\\u0561\\u0580\\u0563\\u0565\\u0580 \\u0587 \\u056F\\u0561\\u0576\\u0561\\u0579\\u0561\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u0413\\u0430\\u0437\\u043E\\u043D\\u044B \\u0438 \\u043E\\u0437\\u0435\\u043B\\u0435\\u043D\\u0435\\u043D\\u0438\\u0435\",\"en\":\"Lawns and planting\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-lawns", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011504"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0548\\u057C\\u0578\\u0563\\u0574\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u0421\\u0438\\u0441\\u0442\\u0435\\u043C\\u044B \\u043F\\u043E\\u043B\\u0438\\u0432\\u0430\",\"en\":\"Irrigation systems\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-irrigation", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011505"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053E\\u0561\\u057C\\u0565\\u0580\\u056B \\u0567\\u057F \\u0587 \\u0570\\u0565\\u057C\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u0440\\u0435\\u0437\\u043A\\u0430 \\u0438 \\u0443\\u0434\\u0430\\u043B\\u0435\\u043D\\u0438\\u0435 \\u0434\\u0435\\u0440\\u0435\\u0432\\u044C\\u0435\\u0432\",\"en\":\"Tree pruning and removal\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-trees", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011506"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053C\\u0578\\u0572\\u0561\\u057E\\u0561\\u0566\\u0561\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u0411\\u0430\\u0441\\u0441\\u0435\\u0439\\u043D\\u044B\",\"en\":\"Swimming pools\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-pools", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011507"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0565\\u0576\\u0561\\u057A\\u0561\\u057F\\u0565\\u0580\",\"ru\":\"\\u041F\\u043E\\u0434\\u043F\\u043E\\u0440\\u043D\\u044B\\u0435 \\u0441\\u0442\\u0435\\u043D\\u044B\",\"en\":\"Retaining walls\"}", new Guid("019a0000-0000-7000-8000-000000000115"), "landscaping-retaining-walls", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011601"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0565\\u057D\\u0561\\u0570\\u057D\\u056F\\u0574\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u0412\\u0438\\u0434\\u0435\\u043E\\u043D\\u0430\\u0431\\u043B\\u044E\\u0434\\u0435\\u043D\\u0438\\u0435\",\"en\":\"CCTV\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-cctv", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011602"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0531\\u0566\\u0564\\u0561\\u0576\\u0577\\u0561\\u0576\\u0561\\u0575\\u056B\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u041E\\u0445\\u0440\\u0430\\u043D\\u043D\\u0430\\u044F \\u0441\\u0438\\u0433\\u043D\\u0430\\u043B\\u0438\\u0437\\u0430\\u0446\\u0438\\u044F\",\"en\":\"Alarm systems\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-alarms", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011603"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0534\\u0578\\u0574\\u0578\\u0586\\u0578\\u0576\\u0576\\u0565\\u0580\",\"ru\":\"\\u0414\\u043E\\u043C\\u043E\\u0444\\u043E\\u043D\\u044B\",\"en\":\"Intercoms\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-intercoms", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011604"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0544\\u0578\\u0582\\u057F\\u0584\\u056B \\u057E\\u0565\\u0580\\u0561\\u0570\\u057D\\u056F\\u0574\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u0421\\u0438\\u0441\\u0442\\u0435\\u043C\\u044B \\u043A\\u043E\\u043D\\u0442\\u0440\\u043E\\u043B\\u044F \\u0434\\u043E\\u0441\\u0442\\u0443\\u043F\\u0430\",\"en\":\"Access control\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-access-control", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011605"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053D\\u0565\\u056C\\u0561\\u0581\\u056B \\u057F\\u0578\\u0582\\u0576\",\"ru\":\"\\u0423\\u043C\\u043D\\u044B\\u0439 \\u0434\\u043E\\u043C\",\"en\":\"Smart home\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-smart-home", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011606"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0579\\u0561\\u0575\\u056B\\u0576 \\u0581\\u0561\\u0576\\u0581\\u0565\\u0580 \\u0587 \\u056B\\u0576\\u057F\\u0565\\u0580\\u0576\\u0565\\u057F\",\"ru\":\"\\u041A\\u043E\\u043C\\u043F\\u044C\\u044E\\u0442\\u0435\\u0440\\u043D\\u044B\\u0435 \\u0441\\u0435\\u0442\\u0438 \\u0438 \\u0438\\u043D\\u0442\\u0435\\u0440\\u043D\\u0435\\u0442\",\"en\":\"Network and internet cabling\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-networks", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011607"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0580\\u0564\\u0565\\u0570\\u0561\\u0575\\u056B\\u0576 \\u0561\\u0566\\u0564\\u0561\\u0576\\u0577\\u0561\\u0576\\u0561\\u0575\\u056B\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\",\"ru\":\"\\u041F\\u043E\\u0436\\u0430\\u0440\\u043D\\u0430\\u044F \\u0441\\u0438\\u0433\\u043D\\u0430\\u043B\\u0438\\u0437\\u0430\\u0446\\u0438\\u044F\",\"en\":\"Fire alarm systems\"}", new Guid("019a0000-0000-7000-8000-000000000116"), "security-fire-alarms", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011701"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0543\\u0561\\u0580\\u057F\\u0561\\u0580\\u0561\\u057A\\u0565\\u057F\\u0561\\u056F\\u0561\\u0576 \\u0576\\u0561\\u056D\\u0561\\u0563\\u056E\\u0578\\u0582\\u0574\",\"ru\":\"\\u0410\\u0440\\u0445\\u0438\\u0442\\u0435\\u043A\\u0442\\u0443\\u0440\\u043D\\u043E\\u0435 \\u043F\\u0440\\u043E\\u0435\\u043A\\u0442\\u0438\\u0440\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435\",\"en\":\"Architectural design\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-architecture", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011702"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053B\\u0576\\u057F\\u0565\\u0580\\u056B\\u0565\\u0580\\u056B \\u0564\\u056B\\u0566\\u0561\\u0575\\u0576\",\"ru\":\"\\u0414\\u0438\\u0437\\u0430\\u0439\\u043D \\u0438\\u043D\\u0442\\u0435\\u0440\\u044C\\u0435\\u0440\\u0430\",\"en\":\"Interior design\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-interior", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011703"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0578\\u0576\\u057D\\u057F\\u0580\\u0578\\u0582\\u056F\\u057F\\u056B\\u057E \\u0576\\u0561\\u056D\\u0561\\u0563\\u056E\\u0578\\u0582\\u0574\",\"ru\":\"\\u041A\\u043E\\u043D\\u0441\\u0442\\u0440\\u0443\\u043A\\u0442\\u0438\\u0432\\u043D\\u043E\\u0435 \\u043F\\u0440\\u043E\\u0435\\u043A\\u0442\\u0438\\u0440\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435\",\"en\":\"Structural engineering\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-structural", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011704"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053B\\u0576\\u056A\\u0565\\u0576\\u0565\\u0580\\u0561\\u056F\\u0561\\u0576 \\u0570\\u0561\\u0574\\u0561\\u056F\\u0561\\u0580\\u0563\\u0565\\u0580\\u056B \\u0576\\u0561\\u056D\\u0561\\u0563\\u056E\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u0440\\u043E\\u0435\\u043A\\u0442\\u0438\\u0440\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435 \\u0438\\u043D\\u0436\\u0435\\u043D\\u0435\\u0440\\u043D\\u044B\\u0445 \\u0441\\u0438\\u0441\\u0442\\u0435\\u043C\",\"en\":\"Building services design\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-building-services", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011705"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0546\\u0561\\u056D\\u0561\\u0570\\u0561\\u0577\\u056B\\u057E\\u0576\\u0565\\u0580\",\"ru\":\"\\u0421\\u043C\\u0435\\u0442\\u044B\",\"en\":\"Cost estimates\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-estimates", 5, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011706"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u054F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\\u056F\\u0561\\u0576 \\u0570\\u057D\\u056F\\u0578\\u0572\\u0578\\u0582\\u0569\\u0575\\u0578\\u0582\\u0576\",\"ru\":\"\\u0422\\u0435\\u0445\\u043D\\u0438\\u0447\\u0435\\u0441\\u043A\\u0438\\u0439 \\u043D\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Construction supervision\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-supervision", 6, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011707"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0549\\u0561\\u0583\\u0561\\u0563\\u0580\\u0578\\u0582\\u0574 \\u0587 \\u0570\\u0565\\u057F\\u0561\\u0566\\u0576\\u0576\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u043C\\u0435\\u0440\\u044B \\u0438 \\u043E\\u0431\\u0441\\u043B\\u0435\\u0434\\u043E\\u0432\\u0430\\u043D\\u0438\\u0435\",\"en\":\"Surveys and measurements\"}", new Guid("019a0000-0000-7000-8000-000000000117"), "design-surveys", 7, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011801"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0544\\u0561\\u0576\\u0580 \\u057E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0574\\u0561\\u0576 \\u0561\\u0577\\u056D\\u0561\\u057F\\u0561\\u0576\\u0584\\u0576\\u0565\\u0580\",\"ru\":\"\\u041C\\u0435\\u043B\\u043A\\u0438\\u0435 \\u0440\\u0435\\u043C\\u043E\\u043D\\u0442\\u043D\\u044B\\u0435 \\u0440\\u0430\\u0431\\u043E\\u0442\\u044B\",\"en\":\"Small repairs\"}", new Guid("019a0000-0000-7000-8000-000000000118"), "handyman-repairs", 1, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011802"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0561\\u056D\\u0578\\u0582\\u0574 \\u0587 \\u0561\\u0574\\u0580\\u0561\\u0581\\u0578\\u0582\\u0574 (\\u0570\\u0565\\u057C\\u0578\\u0582\\u057D\\u057F\\u0561\\u0581\\u0578\\u0582\\u0575\\u0581, \\u0564\\u0561\\u0580\\u0561\\u056F\\u0576\\u0565\\u0580, \\u057E\\u0561\\u0580\\u0561\\u0563\\u0578\\u0582\\u0575\\u0580\\u0576\\u0565\\u0580)\",\"ru\":\"\\u041D\\u0430\\u0432\\u0435\\u0441\\u043A\\u0430 (\\u0442\\u0435\\u043B\\u0435\\u0432\\u0438\\u0437\\u043E\\u0440, \\u043F\\u043E\\u043B\\u043A\\u0438, \\u043A\\u0430\\u0440\\u043D\\u0438\\u0437\\u044B)\",\"en\":\"Mounting (TV, shelves, curtain rails)\"}", new Guid("019a0000-0000-7000-8000-000000000118"), "handyman-mounting", 2, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011803"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0565\\u0576\\u0581\\u0561\\u0572\\u0561\\u0575\\u056B\\u0576 \\u057F\\u0565\\u056D\\u0576\\u056B\\u056F\\u0561\\u0575\\u056B \\u0574\\u056B\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u041F\\u043E\\u0434\\u043A\\u043B\\u044E\\u0447\\u0435\\u043D\\u0438\\u0435 \\u0431\\u044B\\u0442\\u043E\\u0432\\u043E\\u0439 \\u0442\\u0435\\u0445\\u043D\\u0438\\u043A\\u0438\",\"en\":\"Appliance installation\"}", new Guid("019a0000-0000-7000-8000-000000000118"), "handyman-appliances", 3, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011804"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u053F\\u0578\\u0572\\u057A\\u0565\\u0584\\u0576\\u0565\\u0580\\u056B \\u057F\\u0565\\u0572\\u0561\\u0564\\u0580\\u0578\\u0582\\u0574 \\u0587 \\u0583\\u0578\\u056D\\u0561\\u0580\\u056B\\u0576\\u0578\\u0582\\u0574\",\"ru\":\"\\u0423\\u0441\\u0442\\u0430\\u043D\\u043E\\u0432\\u043A\\u0430 \\u0438 \\u0437\\u0430\\u043C\\u0435\\u043D\\u0430 \\u0437\\u0430\\u043C\\u043A\\u043E\\u0432\",\"en\":\"Lock installation and replacement\"}", new Guid("019a0000-0000-7000-8000-000000000118"), "handyman-locks", 4, null, null },
                    { new Guid("019a0000-0000-7000-8000-000000011805"), new DateTimeOffset(new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, null, null, true, false, "{\"hy\":\"\\u0540\\u0565\\u0580\\u0574\\u0565\\u057F\\u056B\\u056F\\u0561\\u0581\\u0578\\u0582\\u0574\",\"ru\":\"\\u0413\\u0435\\u0440\\u043C\\u0435\\u0442\\u0438\\u0437\\u0430\\u0446\\u0438\\u044F \\u0448\\u0432\\u043E\\u0432\",\"en\":\"Sealing and caulking\"}", new Guid("019a0000-0000-7000-8000-000000000118"), "handyman-sealing", 5, null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010101"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010102"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010103"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010104"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010105"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010106"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010107"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010108"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010201"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010202"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010203"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010204"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010205"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010206"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010207"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010208"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010209"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010210"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010211"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010212"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010213"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010301"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010302"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010303"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010304"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010305"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010306"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010307"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010401"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010402"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010403"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010404"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010405"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010406"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010407"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010501"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010502"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010503"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010504"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010505"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010506"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010507"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010508"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010601"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010602"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010603"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010604"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010605"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010606"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010607"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010701"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010702"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010703"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010704"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010705"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010706"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010707"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010708"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010709"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010710"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010711"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010712"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010801"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010802"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010803"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010804"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010805"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010806"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010901"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010902"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010903"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010904"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010905"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010906"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010907"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000010908"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011001"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011002"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011003"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011004"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011005"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011101"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011102"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011103"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011104"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011105"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011201"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011202"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011203"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011204"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011205"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011206"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011207"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011301"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011302"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011303"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011304"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011305"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011306"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011307"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011401"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011402"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011403"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011404"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011405"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011406"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011407"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011501"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011502"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011503"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011504"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011505"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011506"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011507"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011601"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011602"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011603"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011604"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011605"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011606"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011607"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011701"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011702"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011703"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011704"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011705"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011706"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011707"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011801"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011802"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011803"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011804"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000011805"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000108"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000109"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000110"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000111"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000112"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000113"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000114"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000115"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000116"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000117"));

            migrationBuilder.DeleteData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000118"));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000102"),
                column: "name",
                value: "{\"hy\":\"\\u054E\\u0565\\u0580\\u0561\\u0576\\u0578\\u0580\\u0578\\u0563\\u0578\\u0582\\u0574\",\"ru\":\"\\u0420\\u0435\\u043C\\u043E\\u043D\\u0442\",\"en\":\"Renovation\"}");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000106"),
                column: "name",
                value: "{\"hy\":\"\\u0556\\u0561\\u057D\\u0561\\u0564\\u056B \\u0565\\u0580\\u0565\\u057D\\u057A\\u0561\\u057F\\u0578\\u0582\\u0574\",\"ru\":\"\\u041E\\u0431\\u043B\\u0438\\u0446\\u043E\\u0432\\u043A\\u0430 \\u0444\\u0430\\u0441\\u0430\\u0434\\u0430\",\"en\":\"Exterior cladding\"}");
        }
    }
}
