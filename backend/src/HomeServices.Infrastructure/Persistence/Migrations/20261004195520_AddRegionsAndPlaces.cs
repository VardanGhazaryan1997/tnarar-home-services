using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRegionsAndPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_partner_areas_partner_profile_id_city_id_district_id",
                table: "partner_areas");

            migrationBuilder.AlterColumn<Guid>(
                name: "city_id",
                table: "partner_areas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "region_id",
                table: "partner_areas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "cities",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "region_id",
                table: "cities",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "regions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regions", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000201"),
                columns: new[] { "kind", "region_id" },
                values: new object[] { "City", new Guid("019a0000-0000-7000-8000-000000000401") });

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000202"),
                columns: new[] { "kind", "region_id", "sort_order" },
                values: new object[] { "City", new Guid("019a0000-0000-7000-8000-000000000404"), 219 });

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000203"),
                columns: new[] { "kind", "region_id", "sort_order" },
                values: new object[] { "City", new Guid("019a0000-0000-7000-8000-000000000406"), 226 });

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000204"),
                columns: new[] { "kind", "region_id", "sort_order" },
                values: new object[] { "City", new Guid("019a0000-0000-7000-8000-000000000402"), 2 });

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000205"),
                columns: new[] { "kind", "region_id", "sort_order" },
                values: new object[] { "City", new Guid("019a0000-0000-7000-8000-000000000403"), 123 });

            migrationBuilder.InsertData(
                table: "regions",
                columns: new[] { "id", "name", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("019a0000-0000-7000-8000-000000000401"), "{\"hy\":\"\\u0535\\u0580\\u0587\\u0561\\u0576\",\"ru\":\"\\u0415\\u0440\\u0435\\u0432\\u0430\\u043D\",\"en\":\"Yerevan\"}", "yerevan", 1 },
                    { new Guid("019a0000-0000-7000-8000-000000000402"), "{\"hy\":\"\\u0531\\u0580\\u0561\\u0563\\u0561\\u056E\\u0578\\u057F\\u0576\",\"ru\":\"\\u0410\\u0440\\u0430\\u0433\\u0430\\u0446\\u043E\\u0442\\u043D\",\"en\":\"Aragatsotn\"}", "aragatsotn", 2 },
                    { new Guid("019a0000-0000-7000-8000-000000000403"), "{\"hy\":\"\\u0531\\u0580\\u0561\\u0580\\u0561\\u057F\",\"ru\":\"\\u0410\\u0440\\u0430\\u0440\\u0430\\u0442\",\"en\":\"Ararat\"}", "ararat", 3 },
                    { new Guid("019a0000-0000-7000-8000-000000000404"), "{\"hy\":\"\\u0531\\u0580\\u0574\\u0561\\u057E\\u056B\\u0580\",\"ru\":\"\\u0410\\u0440\\u043C\\u0430\\u0432\\u0438\\u0440\",\"en\":\"Armavir\"}", "armavir", 4 },
                    { new Guid("019a0000-0000-7000-8000-000000000405"), "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0580\\u0584\\u0578\\u0582\\u0576\\u056B\\u0584\",\"ru\":\"\\u0413\\u0435\\u0433\\u0430\\u0440\\u043A\\u0443\\u043D\\u0438\\u043A\",\"en\":\"Gegharkunik\"}", "gegharkunik", 5 },
                    { new Guid("019a0000-0000-7000-8000-000000000406"), "{\"hy\":\"\\u053F\\u0578\\u057F\\u0561\\u0575\\u0584\",\"ru\":\"\\u041A\\u043E\\u0442\\u0430\\u0439\\u043A\",\"en\":\"Kotayk\"}", "kotayk", 6 },
                    { new Guid("019a0000-0000-7000-8000-000000000407"), "{\"hy\":\"\\u053C\\u0578\\u057C\\u056B\",\"ru\":\"\\u041B\\u043E\\u0440\\u0438\",\"en\":\"Lori\"}", "lori", 7 },
                    { new Guid("019a0000-0000-7000-8000-000000000408"), "{\"hy\":\"\\u0547\\u056B\\u0580\\u0561\\u056F\",\"ru\":\"\\u0428\\u0438\\u0440\\u0430\\u043A\",\"en\":\"Shirak\"}", "shirak", 8 },
                    { new Guid("019a0000-0000-7000-8000-000000000409"), "{\"hy\":\"\\u054D\\u0575\\u0578\\u0582\\u0576\\u056B\\u0584\",\"ru\":\"\\u0421\\u044E\\u043D\\u0438\\u043A\",\"en\":\"Syunik\"}", "syunik", 9 },
                    { new Guid("019a0000-0000-7000-8000-000000000410"), "{\"hy\":\"\\u054F\\u0561\\u057E\\u0578\\u0582\\u0577\",\"ru\":\"\\u0422\\u0430\\u0432\\u0443\\u0448\",\"en\":\"Tavush\"}", "tavush", 10 },
                    { new Guid("019a0000-0000-7000-8000-000000000411"), "{\"hy\":\"\\u054E\\u0561\\u0575\\u0578\\u0581 \\u0571\\u0578\\u0580\",\"ru\":\"\\u0412\\u0430\\u0439\\u043E\\u0446 \\u0414\\u0437\\u043E\\u0440\",\"en\":\"Vayots Dzor\"}", "vayots-dzor", 11 }
                });

            migrationBuilder.InsertData(
                table: "cities",
                columns: new[] { "id", "is_active", "kind", "name", "region_id", "slug", "sort_order" },
                values: new object[,]
                {
                    { new Guid("00a4465c-b1c0-5d60-93ab-a7b20355bf3c"), true, "Village", "{\"hy\":\"\\u0546\\u0565\\u0580\\u0584\\u056B\\u0576 \\u054D\\u0561\\u057D\\u0576\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041D\\u0435\\u0440\\u043A\\u0438\\u043D \\u0421\\u0430\\u0441\\u043D\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Nerkin Sasnashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nerkin-sasnashen", 80 },
                    { new Guid("014ff9d7-3df4-56a7-9fff-48dba75ce2a4"), true, "Village", "{\"hy\":\"\\u0540\\u0576\\u0561\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u041D\\u0430\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Hnaberd\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "hnaberd-aragatsotn", 68 },
                    { new Guid("019bcce5-73c2-5550-ab3a-e261255e9cb9"), true, "Village", "{\"hy\":\"\\u054D\\u056B\\u0583\\u0561\\u0576\\u056B\\u056F\",\"ru\":\"\\u0421\\u0438\\u043F\\u0430\\u043D\\u0438\\u043A\",\"en\":\"Sipanik\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "sipanik", 207 },
                    { new Guid("02236d1b-d32e-55ae-a8f0-ed4249d26d9e"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0578\\u0582\\u0573\",\"ru\":\"\\u0410\\u0440\\u0443\\u0447\",\"en\":\"Aruch\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aruch", 19 },
                    { new Guid("02fab6c0-4667-5da5-bc11-864688661902"), true, "Village", "{\"hy\":\"\\u0555\\u0570\\u0561\\u0576\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u041E\\u0430\\u043D\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Ohanavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ohanavan", 118 },
                    { new Guid("0428c381-46ce-5953-902f-fbcfc7e3b6ca"), true, "Village", "{\"hy\":\"\\u0546\\u0577\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u041D\\u0448\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Nshavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nshavan", 185 },
                    { new Guid("05efe3e1-334d-57fb-81fb-e0666607ddf8"), true, "Village", "{\"hy\":\"\\u0544\\u0565\\u056E\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u041C\\u0435\\u0446\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Metsadzor\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "metsadzor", 76 },
                    { new Guid("060965dc-4c46-5ea9-8c86-7939cbbc6865"), true, "Village", "{\"hy\":\"\\u054B\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0414\\u0436\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Jrashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "jrashen", 202 },
                    { new Guid("07606e42-749c-57ba-bfdf-85ca6117d229"), true, "Village", "{\"hy\":\"\\u0539\\u0569\\u0578\\u0582\\u057B\\u0578\\u0582\\u0580\",\"ru\":\"\\u0422\\u0442\\u0443\\u0434\\u0436\\u0443\\u0440\",\"en\":\"Ttujur\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ttujur", 42 },
                    { new Guid("07708016-2e87-5696-8a9e-2527698974d0"), true, "City", "{\"hy\":\"\\u0531\\u0575\\u0580\\u0578\\u0582\\u0574\",\"ru\":\"\\u0410\\u0439\\u0440\\u0443\\u043C\",\"en\":\"Ayrum\"}", new Guid("019a0000-0000-7000-8000-000000000410"), "ayrum", 251 },
                    { new Guid("07ab13ad-30a1-5535-8001-b6d5e85663f1"), true, "Village", "{\"hy\":\"\\u054D\\u056B\\u057D\",\"ru\":\"\\u0421\\u0438\\u0441\",\"en\":\"Sis\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "sis", 205 },
                    { new Guid("07b3a0f6-d8cb-5787-9af7-7888da49cb58"), true, "Village", "{\"hy\":\"\\u0532\\u0575\\u0578\\u0582\\u0580\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0411\\u044E\\u0440\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Byuravan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "byuravan", 145 },
                    { new Guid("07cba34c-db44-57c0-a1d3-8319a83a57f5"), true, "City", "{\"hy\":\"\\u0531\\u0563\\u0561\\u0580\\u0561\\u056F\",\"ru\":\"\\u0410\\u0433\\u0430\\u0440\\u0430\\u043A\",\"en\":\"Agarak\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "agarak", 244 },
                    { new Guid("0bca866e-ade6-5ed7-9232-4cc3d8328b21"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u057F\\u0561\\u0583\\u0576\\u0575\\u0561\",\"ru\":\"\\u0413\\u0435\\u0442\\u0430\\u043F\\u043D\\u044F\",\"en\":\"Getapnya\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "getapnya", 149 },
                    { new Guid("0c2cbad2-15eb-5e31-9a62-5cb3f3675c8c"), true, "Village", "{\"hy\":\"\\u0544\\u0561\\u0580\\u0574\\u0561\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041C\\u0430\\u0440\\u043C\\u0430\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Marmarashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "marmarashen", 178 },
                    { new Guid("0dcb91eb-7941-57d9-931c-484483083bda"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0563\\u0561\\u057E\\u0561\\u0576\\u0564\",\"ru\":\"\\u0410\\u0440\\u0433\\u0430\\u0432\\u0430\\u043D\\u0434\",\"en\":\"Argavand\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "argavand", 137 },
                    { new Guid("0e63faa9-a808-5d84-903c-28675f0d034d"), true, "City", "{\"hy\":\"\\u0546\\u0578\\u0575\\u0565\\u0574\\u0562\\u0565\\u0580\\u0575\\u0561\\u0576\",\"ru\":\"\\u041D\\u043E\\u0435\\u043C\\u0431\\u0435\\u0440\\u044F\\u043D\",\"en\":\"Noyemberyan\"}", new Guid("019a0000-0000-7000-8000-000000000410"), "noyemberyan", 255 },
                    { new Guid("0e82831b-8cad-5013-b222-29bcd6db6042"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\\u056C\\u0565\\u0566\",\"ru\":\"\\u0410\\u0440\\u0430\\u043B\\u0435\\u0437\",\"en\":\"Aralez\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "aralez", 134 },
                    { new Guid("0fab38a8-3a35-5f3d-bb07-ea6f66d050fe"), true, "Village", "{\"hy\":\"\\u053E\\u0561\\u0572\\u056F\\u0561\\u0570\\u0578\\u057E\\u056B\\u057F\",\"ru\":\"\\u0426\\u0430\\u0445\\u043A\\u0430\\u043E\\u0432\\u0438\\u0442\",\"en\":\"Tsaghkahovit\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tsaghkahovit", 51 },
                    { new Guid("10b7368c-32d3-52e7-ba09-3202057180a5"), true, "Village", "{\"hy\":\"\\u0546\\u0561\\u0580\\u0565\\u056F\",\"ru\":\"\\u041D\\u0430\\u0440\\u0435\\u043A\",\"en\":\"Narek\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "narek", 183 },
                    { new Guid("119bbbe1-2485-539c-a443-13a11b495457"), true, "City", "{\"hy\":\"\\u054D\\u057F\\u0565\\u0583\\u0561\\u0576\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0421\\u0442\\u0435\\u043F\\u0430\\u043D\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Stepanavan\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "stepanavan", 238 },
                    { new Guid("12433fe9-5c31-5c3d-84bd-70ea0dda8d8e"), true, "Village", "{\"hy\":\"\\u0539\\u056C\\u056B\\u056F\",\"ru\":\"\\u0422\\u043B\\u0438\\u043A\",\"en\":\"Tlik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tlik", 43 },
                    { new Guid("1294344f-0208-5d79-b40b-a9ca91089523"), true, "Village", "{\"hy\":\"\\u0532\\u0565\\u0580\\u0584\\u0561\\u057C\\u0561\\u057F\",\"ru\":\"\\u0411\\u0435\\u0440\\u043A\\u0430\\u0440\\u0430\\u0442\",\"en\":\"Berkarat\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "berkarat", 24 },
                    { new Guid("13039b50-9eac-5737-a9e6-3e36a43db088"), true, "City", "{\"hy\":\"\\u053B\\u057B\\u0587\\u0561\\u0576\",\"ru\":\"\\u0418\\u0434\\u0436\\u0435\\u0432\\u0430\\u043D\",\"en\":\"Ijevan\"}", new Guid("019a0000-0000-7000-8000-000000000410"), "ijevan", 254 },
                    { new Guid("139b0843-40f6-5557-b7e8-c33d654daf05"), true, "Village", "{\"hy\":\"\\u0553\\u0578\\u0584\\u0580 \\u054E\\u0565\\u0564\\u056B\",\"ru\":\"\\u041F\\u043E\\u043A\\u0440 \\u0412\\u0435\\u0434\\u0438\",\"en\":\"Pokr Vedi\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "pokr-vedi", 216 },
                    { new Guid("143554fd-dd6d-58d6-baf6-e3bd2be6ccd0"), true, "Village", "{\"hy\":\"\\u0547\\u0572\\u0561\\u0580\\u0577\\u056B\\u056F\",\"ru\":\"\\u0428\\u0445\\u0430\\u0440\\u0448\\u0438\\u043A\",\"en\":\"Shgharshik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "shgharshik", 90 },
                    { new Guid("15532b03-34ea-5a39-b751-ddfbf590fea2"), true, "Village", "{\"hy\":\"\\u0544\\u0580\\u0563\\u0561\\u0576\\u0578\\u0582\\u0577\",\"ru\":\"\\u041C\\u0440\\u0433\\u0430\\u043D\\u0443\\u0448\",\"en\":\"Mrganush\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "mrganush", 180 },
                    { new Guid("16d4a451-6ce2-5097-a97d-fff1300c4a63"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0561\\u0562\\u056C\\u0578\\u0582\\u0580\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0430\\u0431\\u043B\\u0443\\u0440\",\"en\":\"Vardablur\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "vardablur", 109 },
                    { new Guid("18140e93-b939-59ee-b338-e8d6a95ace1c"), true, "City", "{\"hy\":\"\\u0534\\u0561\\u057D\\u057F\\u0561\\u056F\\u0565\\u0580\\u057F\",\"ru\":\"\\u0414\\u0430\\u0441\\u0442\\u0430\\u043A\\u0435\\u0440\\u0442\",\"en\":\"Dastakert\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "dastakert", 246 },
                    { new Guid("19ead0d3-3df0-5354-b750-c1d6df26bf60"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0587\\u0561\\u0562\\u0578\\u0582\\u0575\\u0580\",\"ru\":\"\\u0410\\u0440\\u0435\\u0432\\u0430\\u0431\\u0443\\u0439\\u0440\",\"en\":\"Arevabuyr\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "arevabuyr", 139 },
                    { new Guid("1b42820f-d057-53fd-b2b1-746cd6cd4501"), true, "City", "{\"hy\":\"\\u053F\\u0561\\u057A\\u0561\\u0576\",\"ru\":\"\\u041A\\u0430\\u043F\\u0430\\u043D\",\"en\":\"Kapan\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "kapan", 247 },
                    { new Guid("1bc25562-470d-5790-bb46-198b469b0d14"), true, "City", "{\"hy\":\"\\u0539\\u0561\\u056C\\u056B\\u0576\",\"ru\":\"\\u0422\\u0430\\u043B\\u0438\\u043D\",\"en\":\"Talin\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "talin", 4 },
                    { new Guid("20a7119f-f3a4-56c3-bcbf-fb1522c9f3ae"), true, "Village", "{\"hy\":\"\\u054F\\u0561\\u0583\\u0565\\u0580\\u0561\\u056F\\u0561\\u0576\",\"ru\":\"\\u0422\\u0430\\u043F\\u0435\\u0440\\u0430\\u043A\\u0430\\u043D\",\"en\":\"Taperakan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "taperakan", 214 },
                    { new Guid("23a46024-6cc5-58c0-accb-4e12193f0599"), true, "Village", "{\"hy\":\"\\u0531\\u0577\\u0576\\u0561\\u056F\",\"ru\":\"\\u0410\\u0448\\u043D\\u0430\\u043A\",\"en\":\"Ashnak\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ashnak", 11 },
                    { new Guid("2469896b-1b9d-55e6-98eb-c43472748a92"), true, "Village", "{\"hy\":\"\\u053D\\u0576\\u0578\\u0582\\u057D\\u056B\\u056F\",\"ru\":\"\\u0425\\u043D\\u0443\\u0441\\u0438\\u043A\",\"en\":\"Khnusik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "khnusik", 50 },
                    { new Guid("24e77f68-00c5-5c93-b883-0f7e3ee4e65e"), true, "City", "{\"hy\":\"\\u0531\\u057A\\u0561\\u0580\\u0561\\u0576\",\"ru\":\"\\u0410\\u043F\\u0430\\u0440\\u0430\\u043D\",\"en\":\"Aparan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aparan", 3 },
                    { new Guid("261a6566-81e4-5290-80e9-df86ebe7e2c5"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0562\\u0561\\u0569\",\"ru\":\"\\u0410\\u0440\\u0431\\u0430\\u0442\",\"en\":\"Arbat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "arbat", 136 },
                    { new Guid("26f709ea-0180-54d7-9ee7-6408660ed0d1"), true, "Village", "{\"hy\":\"\\u054D\\u0561\\u0580\\u0561\\u056C\\u0561\\u0576\\u057B\",\"ru\":\"\\u0421\\u0430\\u0440\\u0430\\u043B\\u0430\\u043D\\u0434\\u0436\",\"en\":\"Saralanj\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "saralanj", 105 },
                    { new Guid("2843cf5f-2d88-5d92-b18e-74d4ffc5dbf9"), true, "Village", "{\"hy\":\"\\u0534\\u0574\\u056B\\u057F\\u0580\\u0578\\u057E\",\"ru\":\"\\u0414\\u043C\\u0438\\u0442\\u0440\\u043E\\u0432\",\"en\":\"Dmitrov\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "dmitrov", 159 },
                    { new Guid("2a097690-9eae-54ae-a414-360f4dd8524b"), true, "Village", "{\"hy\":\"\\u0531\\u0566\\u0561\\u057F\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0437\\u0430\\u0442\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Azatavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "azatavan", 127 },
                    { new Guid("2a818d67-01b8-5c52-9957-86133aa46852"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0564\\u056B\\u0580\",\"ru\":\"\\u0413\\u0435\\u0445\\u0430\\u0434\\u0438\\u0440\",\"en\":\"Geghadir\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "geghadir", 27 },
                    { new Guid("2c16b3fa-38b5-5207-9433-d59a7251c325"), true, "Village", "{\"hy\":\"\\u054A\\u0561\\u0580\\u057F\\u056B\\u0566\\u0561\\u056F\",\"ru\":\"\\u041F\\u0430\\u0440\\u0442\\u0438\\u0437\\u0430\\u043A\",\"en\":\"Partizak\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "partizak", 98 },
                    { new Guid("2c18fbaa-9d14-5d51-bf31-63bf5f02d28a"), true, "City", "{\"hy\":\"\\u0539\\u0578\\u0582\\u0574\\u0561\\u0576\\u0575\\u0561\\u0576\",\"ru\":\"\\u0422\\u0443\\u043C\\u0430\\u043D\\u044F\\u043D\",\"en\":\"Tumanyan\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "tumanyan", 235 },
                    { new Guid("2c24c769-cd09-51cf-ad8f-5fb02987ccd2"), true, "Village", "{\"hy\":\"\\u0546\\u056B\\u0566\\u0561\\u0574\\u056B\",\"ru\":\"\\u041D\\u0438\\u0437\\u0430\\u043C\\u0438\",\"en\":\"Nizami\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nizami", 184 },
                    { new Guid("2cf43c58-48a6-541d-8242-ae9812e92e07"), true, "Village", "{\"hy\":\"\\u0544\\u0580\\u0563\\u0561\\u057E\\u0565\\u057F\",\"ru\":\"\\u041C\\u0440\\u0433\\u0430\\u0432\\u0435\\u0442\",\"en\":\"Mrgavet\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "mrgavet", 182 },
                    { new Guid("2dca1947-a8fd-5511-8483-851a3a8dfe60"), true, "Village", "{\"hy\":\"\\u0544\\u056B\\u057B\\u0576\\u0561\\u057F\\u0578\\u0582\\u0576\",\"ru\":\"\\u041C\\u0438\\u0434\\u0436\\u043D\\u0430\\u0442\\u0443\\u043D\",\"en\":\"Mijnatun\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "mijnatun", 77 },
                    { new Guid("2de143cd-6148-574d-b9e0-99caded10b90"), true, "Village", "{\"hy\":\"\\u0544\\u056B\\u0580\\u0561\\u0584\",\"ru\":\"\\u041C\\u0438\\u0440\\u0430\\u043A\",\"en\":\"Mirak\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "mirak", 78 },
                    { new Guid("2e02a995-f5a0-5367-864c-5accc9db9b59"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Vardashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vardashen", 211 },
                    { new Guid("2e9115de-2372-58fa-811c-97a4934ce2f8"), true, "Village", "{\"hy\":\"\\u0548\\u0582\\u0580\\u0581\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u0423\\u0440\\u0446\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Urtsadzor\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "urtsadzor", 199 },
                    { new Guid("3032c355-d15b-5ec6-81f2-89eaa6907569"), true, "Village", "{\"hy\":\"\\u0546\\u056B\\u0563\\u0561\\u057F\\u0578\\u0582\\u0576\",\"ru\":\"\\u041D\\u0438\\u0433\\u0430\\u0442\\u0443\\u043D\",\"en\":\"Nigatun\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nigatun", 82 },
                    { new Guid("3044dd4d-bbf7-5a69-ad84-d309b8b210b1"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0569\\u0576\\u0561\\u0572\\u0562\\u0575\\u0578\\u0582\\u0580\",\"ru\":\"\\u041A\\u0430\\u0442\\u043D\\u0430\\u0445\\u0431\\u044E\\u0440\",\"en\":\"Katnaghbyur\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "katnaghbyur", 56 },
                    { new Guid("32a8a66f-48ee-51d3-9155-b8642edd77e6"), true, "City", "{\"hy\":\"\\u054D\\u056B\\u057D\\u056B\\u0561\\u0576\",\"ru\":\"\\u0421\\u0438\\u0441\\u0438\\u0430\\u043D\",\"en\":\"Sisian\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "sisian", 249 },
                    { new Guid("34c8cdd2-34be-5dfd-a358-9f75d0c27ddd"), true, "Village", "{\"hy\":\"\\u054D\\u0578\\u0582\\u057D\\u0565\\u0580\",\"ru\":\"\\u0421\\u0443\\u0441\\u0435\\u0440\",\"en\":\"Suser\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "suser", 108 },
                    { new Guid("3683e64d-01e7-5ebe-99d0-e615070acd68"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0576\\u056B\\u0561\\u0577\\u056B\\u0580\",\"ru\":\"\\u041A\\u0430\\u043D\\u0438\\u0430\\u0448\\u0438\\u0440\",\"en\":\"Kaniashir\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kaniashir", 58 },
                    { new Guid("36ef1397-37ee-5542-b162-07691c2c8faf"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u0580\\u0561\\u056F\\u0565\\u0580\\u057F\",\"ru\":\"\\u0414\\u0430\\u0440\\u0430\\u043A\\u0435\\u0440\\u0442\",\"en\":\"Darakert\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "darakert", 155 },
                    { new Guid("39b551ae-28f7-5ec1-978e-31b20e088bd5"), true, "City", "{\"hy\":\"\\u054D\\u057A\\u056B\\u057F\\u0561\\u056F\",\"ru\":\"\\u0421\\u043F\\u0438\\u0442\\u0430\\u043A\",\"en\":\"Spitak\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "spitak", 237 },
                    { new Guid("3b31dd71-c38e-553e-b78f-c1ba4e752a7c"), true, "Village", "{\"hy\":\"\\u053E\\u0561\\u0572\\u056F\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0426\\u0430\\u0445\\u043A\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Tsaghkashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tsaghkashen", 52 },
                    { new Guid("3b4b58ef-dcf4-5ed6-b1e9-b24d95b44dc3"), true, "Village", "{\"hy\":\"\\u0540\\u0561\\u0581\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0410\\u0446\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Hatsashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "hatsashen", 67 },
                    { new Guid("3cf4f891-5e18-5e43-8361-e88ec9cc6089"), true, "Village", "{\"hy\":\"\\u0532\\u0575\\u0578\\u0582\\u0580\\u0561\\u056F\\u0561\\u0576\",\"ru\":\"\\u0411\\u044E\\u0440\\u0430\\u043A\\u0430\\u043D\",\"en\":\"Byurakan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "byurakan", 25 },
                    { new Guid("3dbad1d2-721f-5d77-acb9-c35a6690d998"), true, "Village", "{\"hy\":\"\\u0555\\u0577\\u0561\\u056F\\u0561\\u0576\",\"ru\":\"\\u041E\\u0448\\u0430\\u043A\\u0430\\u043D\",\"en\":\"Oshakan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "oshakan", 119 },
                    { new Guid("3ee46a93-cfde-5bf3-a599-9e74dcdacab2"), true, "City", "{\"hy\":\"\\u054E\\u0561\\u0575\\u0584\",\"ru\":\"\\u0412\\u0430\\u0439\\u043A\",\"en\":\"Vayk\"}", new Guid("019a0000-0000-7000-8000-000000000411"), "vayk", 258 },
                    { new Guid("3fd81473-cccb-5fba-a8db-53c472b69b8e"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u057F\\u0561\\u0583\",\"ru\":\"\\u0413\\u0435\\u0442\\u0430\\u043F\",\"en\":\"Getap\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "getap", 30 },
                    { new Guid("41dda536-f6c7-5dab-b5ca-2febca2debb3"), true, "Village", "{\"hy\":\"\\u0531\\u0575\\u0563\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0439\\u0433\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Aygavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "aygavan", 128 },
                    { new Guid("42070bf9-87b2-54c2-89c7-f673703dcdec"), true, "City", "{\"hy\":\"\\u0547\\u0561\\u0574\\u056C\\u0578\\u0582\\u0572\",\"ru\":\"\\u0428\\u0430\\u043C\\u043B\\u0443\\u0433\",\"en\":\"Shamlugh\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "shamlugh", 236 },
                    { new Guid("421bac50-c125-5533-af1a-0cc87eb4317f"), true, "Village", "{\"hy\":\"\\u0534\\u057E\\u056B\\u0576\",\"ru\":\"\\u0414\\u0432\\u0438\\u043D\",\"en\":\"Dvin\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "dvin", 160 },
                    { new Guid("42bbe1d0-4214-5301-9635-df6d95b63379"), true, "Village", "{\"hy\":\"\\u0548\\u0582\\u0580\\u0581\\u0561\\u056C\\u0561\\u0576\\u057B\",\"ru\":\"\\u0423\\u0440\\u0446\\u0430\\u043B\\u0430\\u043D\\u0434\\u0436\",\"en\":\"Urtsalanj\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "urtsalanj", 198 },
                    { new Guid("42dc5b29-fe91-5608-93d1-712630751789"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u0580\\u0562\\u0576\\u056B\\u056F\",\"ru\":\"\\u0414\\u0430\\u0440\\u0431\\u043D\\u0438\\u043A\",\"en\":\"Darbnik\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "darbnik", 156 },
                    { new Guid("433e9a13-d92f-5d17-a729-ac5addb0675a"), true, "City", "{\"hy\":\"\\u0535\\u0572\\u0565\\u0563\\u0576\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u0415\\u0445\\u0435\\u0433\\u043D\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Yeghegnadzor\"}", new Guid("019a0000-0000-7000-8000-000000000411"), "yeghegnadzor", 256 },
                    { new Guid("43d84ec9-40bc-5072-bf7a-ba5fa2b86a54"), true, "City", "{\"hy\":\"\\u0533\\u0578\\u0580\\u056B\\u057D\",\"ru\":\"\\u0413\\u043E\\u0440\\u0438\\u0441\",\"en\":\"Goris\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "goris", 245 },
                    { new Guid("44ffdce8-337d-5ea3-8e83-4ff4b25c362c"), true, "Village", "{\"hy\":\"\\u0532\\u0565\\u0580\\u0564\\u056B\\u056F\",\"ru\":\"\\u0411\\u0435\\u0440\\u0434\\u0438\\u043A\",\"en\":\"Berdik\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "berdik", 143 },
                    { new Guid("482fd2f5-55c7-5599-a463-ecf3849aeaa6"), true, "Village", "{\"hy\":\"\\u0534\\u056B\\u0561\\u0576\",\"ru\":\"\\u0414\\u0438\\u0430\\u043D\",\"en\":\"Dian\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dian", 33 },
                    { new Guid("4893ee72-6946-513c-a167-395b097a5cc8"), true, "Village", "{\"hy\":\"\\u054D\\u0561\\u0575\\u0561\\u0569-\\u0546\\u0578\\u057E\\u0561\",\"ru\":\"\\u0421\\u0430\\u044F\\u0442-\\u041D\\u043E\\u0432\\u0430\",\"en\":\"Sayat-Nova\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "sayat-nova", 204 },
                    { new Guid("4a59cd69-32b8-51c8-a8d7-d7e96c90f884"), true, "City", "{\"hy\":\"\\u0531\\u0580\\u057F\\u0561\\u0577\\u0561\\u057F\",\"ru\":\"\\u0410\\u0440\\u0442\\u0430\\u0448\\u0430\\u0442\",\"en\":\"Artashat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "artashat", 122 },
                    { new Guid("4c7e11dd-4cf9-531e-8339-8187a91437d8"), true, "Village", "{\"hy\":\"\\u0531\\u056C\\u0561\\u0563\\u0575\\u0561\\u0566\",\"ru\":\"\\u0410\\u043B\\u0430\\u0433\\u044F\\u0437\",\"en\":\"Alagyaz\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "alagyaz", 7 },
                    { new Guid("4d3109eb-c4e1-5bb2-879b-37653dab7703"), true, "Village", "{\"hy\":\"\\u0539\\u0561\\u0569\\u0578\\u0582\\u056C\",\"ru\":\"\\u0422\\u0430\\u0442\\u0443\\u043B\",\"en\":\"Tatul\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tatul", 40 },
                    { new Guid("4fe1f71e-a19a-540c-bb6f-37eac034ba00"), true, "Village", "{\"hy\":\"\\u0534\\u056B\\u057F\\u0561\\u056F\",\"ru\":\"\\u0414\\u0438\\u0442\\u0430\\u043A\",\"en\":\"Ditak\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ditak", 158 },
                    { new Guid("5252557a-2ad4-52f7-a317-a1f6e6c09d8a"), true, "Village", "{\"hy\":\"\\u0531\\u0575\\u0563\\u0565\\u0566\\u0561\\u0580\\u0564\",\"ru\":\"\\u0410\\u0439\\u0433\\u0435\\u0437\\u0430\\u0440\\u0434\",\"en\":\"Aygezard\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "aygezard", 129 },
                    { new Guid("5286d114-5a60-5bda-80f6-cfaef43ad2ff"), true, "City", "{\"hy\":\"\\u054E\\u0561\\u0576\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u0412\\u0430\\u043D\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Vanadzor\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "vanadzor", 239 },
                    { new Guid("538d736d-c261-5f67-bc9f-85f5123fa10c"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u057F\\u0565\\u0576\\u056B\",\"ru\":\"\\u0410\\u0440\\u0442\\u0435\\u043D\\u0438\",\"en\":\"Arteni\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "arteni", 21 },
                    { new Guid("5434bca5-2cf9-5a14-87fd-03712fb824bd"), true, "City", "{\"hy\":\"\\u0531\\u0580\\u0569\\u056B\\u056F\",\"ru\":\"\\u0410\\u0440\\u0442\\u0438\\u043A\",\"en\":\"Artik\"}", new Guid("019a0000-0000-7000-8000-000000000408"), "artik", 241 },
                    { new Guid("550a4281-1cdc-5f12-b698-a92dfd546e99"), true, "Village", "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u0572\\u0562\\u0575\\u0578\\u0582\\u0580\",\"ru\":\"\\u041B\\u0443\\u0441\\u0430\\u0445\\u0431\\u044E\\u0440\",\"en\":\"Lusaghbyur\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "lusaghbyur", 49 },
                    { new Guid("553363a1-757e-5caf-9525-f860c50da5ae"), true, "Village", "{\"hy\":\"\\u0544\\u0580\\u0563\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u041C\\u0440\\u0433\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Mrgavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "mrgavan", 181 },
                    { new Guid("58246618-fb31-53d0-9dfc-dd732eff87cc"), true, "Village", "{\"hy\":\"\\u0548\\u0582\\u0577\\u056B\",\"ru\":\"\\u0423\\u0448\\u0438\",\"en\":\"Ushi\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ushi", 95 },
                    { new Guid("5854d40c-bded-5bbb-9662-c78d39526851"), true, "City", "{\"hy\":\"\\u0543\\u0561\\u0574\\u0562\\u0561\\u0580\\u0561\\u056F\",\"ru\":\"\\u0427\\u0430\\u043C\\u0431\\u0430\\u0440\\u0430\\u043A\",\"en\":\"Chambarak\"}", new Guid("019a0000-0000-7000-8000-000000000405"), "chambarak", 222 },
                    { new Guid("58aaa51a-a682-5689-bc69-9a98f10ccb17"), true, "City", "{\"hy\":\"\\u0540\\u0580\\u0561\\u0566\\u0564\\u0561\\u0576\",\"ru\":\"\\u0420\\u0430\\u0437\\u0434\\u0430\\u043D\",\"en\":\"Hrazdan\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "hrazdan", 230 },
                    { new Guid("59b15bcd-fd94-59e2-83c0-2974d4da2065"), true, "Village", "{\"hy\":\"\\u0547\\u0578\\u0572\\u0561\\u056F\\u0576\",\"ru\":\"\\u0428\\u043E\\u0445\\u0430\\u043A\\u043D\",\"en\":\"Shoghakn\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "shoghakn", 91 },
                    { new Guid("5b110bc4-c0de-50f3-b0ac-3c94b46140b8"), true, "Village", "{\"hy\":\"\\u0532\\u0561\\u0580\\u0571\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0411\\u0430\\u0440\\u0434\\u0437\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Bardzrashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "bardzrashen", 142 },
                    { new Guid("5b1be49e-9bb1-5464-b3ad-5c86e1b60636"), true, "Village", "{\"hy\":\"\\u053F\\u0578\\u0577\",\"ru\":\"\\u041A\\u043E\\u0448\",\"en\":\"Kosh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kosh", 64 },
                    { new Guid("5bcba987-399c-5b51-b59c-af58df0bb800"), true, "Village", "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u0563\\u0575\\u0578\\u0582\\u0572\",\"ru\":\"\\u041B\\u0443\\u0441\\u0430\\u0433\\u044E\\u0445\",\"en\":\"Lusagyugh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "lusagyugh", 47 },
                    { new Guid("5cd595cb-8e10-5429-8ecb-0908b3147f73"), true, "Village", "{\"hy\":\"\\u053C\\u0565\\u057C\\u0576\\u0561\\u057C\\u0578\\u057F\",\"ru\":\"\\u041B\\u0435\\u0440\\u043D\\u0430\\u0440\\u043E\\u0442\",\"en\":\"Lernarot\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "lernarot", 46 },
                    { new Guid("5cd71c59-e2a2-55b1-bec7-fb737271927b"), true, "Village", "{\"hy\":\"\\u0531\\u0563\\u0561\\u0580\\u0561\\u056F\",\"ru\":\"\\u0410\\u0433\\u0430\\u0440\\u0430\\u043A\",\"en\":\"Agarak\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "agarak-aragatsotn", 5 },
                    { new Guid("5e8030dc-5803-5c34-82d9-092ae62638d9"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\\u0563\\u0561\\u056E\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0440\\u0430\\u0433\\u0430\\u0446\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Aragatsavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aragatsavan", 17 },
                    { new Guid("5f1ad1ae-45b8-5472-8686-7e407a39a67c"), true, "Village", "{\"hy\":\"\\u0531\\u0566\\u0561\\u057F\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0410\\u0437\\u0430\\u0442\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Azatashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "azatashen", 126 },
                    { new Guid("5fb4c60f-795d-5715-b45c-6b57e8534741"), true, "Village", "{\"hy\":\"\\u0543\\u0584\\u0576\\u0561\\u0572\",\"ru\":\"\\u0427\\u043A\\u043D\\u0430\\u0445\",\"en\":\"Chknagh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "chknagh", 73 },
                    { new Guid("60b576ee-f510-5b5b-9d79-a6626df3a15b"), true, "Village", "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u056F\\u0576\",\"ru\":\"\\u041B\\u0443\\u0441\\u0430\\u043A\\u043D\",\"en\":\"Lusakn\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "lusakn", 48 },
                    { new Guid("60f91c32-b650-538c-91d8-2cf37bbfc46c"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0531\\u0580\\u0569\\u056B\\u056F\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0410\\u0440\\u0442\\u0438\\u043A\",\"en\":\"Nor Artik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nor-artik", 84 },
                    { new Guid("62d39bee-555b-53b1-a5f0-a4948317cd13"), true, "City", "{\"hy\":\"\\u054E\\u0565\\u0564\\u056B\",\"ru\":\"\\u0412\\u0435\\u0434\\u0438\",\"en\":\"Vedi\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vedi", 124 },
                    { new Guid("64368665-30c1-5d35-b20e-2003d3a402a3"), true, "Village", "{\"hy\":\"\\u053C\\u0565\\u057C\\u0576\\u0561\\u057A\\u0561\\u0580\",\"ru\":\"\\u041B\\u0435\\u0440\\u043D\\u0430\\u043F\\u0430\\u0440\",\"en\":\"Lernapar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "lernapar", 45 },
                    { new Guid("64f9be27-773e-57f6-9811-b84fe0d5ed3a"), true, "Village", "{\"hy\":\"\\u0532\\u0565\\u0580\\u0584\\u0561\\u0576\\u0578\\u0582\\u0577\",\"ru\":\"\\u0411\\u0435\\u0440\\u043A\\u0430\\u043D\\u0443\\u0448\",\"en\":\"Berkanush\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "berkanush", 144 },
                    { new Guid("651e1636-6c21-547b-9cb6-6518f93ff173"), true, "Village", "{\"hy\":\"\\u0541\\u0578\\u0580\\u0561\\u0563\\u0575\\u0578\\u0582\\u0572\",\"ru\":\"\\u0414\\u0437\\u043E\\u0440\\u0430\\u0433\\u044E\\u0445\",\"en\":\"Dzoragyugh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dzoragyugh", 71 },
                    { new Guid("6677cc07-aeae-508e-8164-03e21e9b785e"), true, "Village", "{\"hy\":\"\\u0554\\u0578\\u0582\\u0579\\u0561\\u056F\",\"ru\":\"\\u041A\\u0443\\u0447\\u0430\\u043A\",\"en\":\"Kuchak\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kuchak", 116 },
                    { new Guid("671d4e99-e9e9-56b3-8ae2-52406d463860"), true, "City", "{\"hy\":\"\\u0544\\u0565\\u056E\\u0561\\u0574\\u0578\\u0580\",\"ru\":\"\\u041C\\u0435\\u0446\\u0430\\u043C\\u043E\\u0440\",\"en\":\"Metsamor\"}", new Guid("019a0000-0000-7000-8000-000000000404"), "metsamor", 220 },
                    { new Guid("6900aa5a-d35f-55e1-a34d-35c709714043"), true, "City", "{\"hy\":\"\\u0554\\u0561\\u057B\\u0561\\u0580\\u0561\\u0576\",\"ru\":\"\\u041A\\u0430\\u0434\\u0436\\u0430\\u0440\\u0430\\u043D\",\"en\":\"Kajaran\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "kajaran", 250 },
                    { new Guid("69a17692-8245-5071-8c18-13767991c563"), true, "Village", "{\"hy\":\"\\u0533\\u0561\\u057C\\u0576\\u0561\\u0570\\u0578\\u057E\\u056B\\u057F\",\"ru\":\"\\u0413\\u0430\\u0440\\u043D\\u0430\\u043E\\u0432\\u0438\\u0442\",\"en\":\"Garnahovit\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "garnahovit", 26 },
                    { new Guid("6ad91b38-02a5-5325-afd6-9d8d317e82cd"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0580\\u0574\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041A\\u0430\\u0440\\u043C\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Karmrashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "karmrashen", 62 },
                    { new Guid("6aebb208-6c1c-5813-a8e8-0a21c4423fe4"), true, "Village", "{\"hy\":\"\\u0534\\u0565\\u0572\\u0571\\u0578\\u0582\\u057F\",\"ru\":\"\\u0414\\u0435\\u0445\\u0434\\u0437\\u0443\\u0442\",\"en\":\"Deghdzut\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "deghdzut", 157 },
                    { new Guid("6bc3ed51-ce10-5e92-b17e-1c3345f16787"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0580\\u0578\\u057F\",\"ru\":\"\\u0413\\u0435\\u0445\\u0430\\u0440\\u043E\\u0442\",\"en\":\"Gegharot\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "gegharot", 29 },
                    { new Guid("6d0a0938-76e9-5975-9627-6b42a9ac1969"), true, "Village", "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u0577\\u0578\\u0572\",\"ru\":\"\\u041B\\u0443\\u0441\\u0430\\u0448\\u043E\\u0445\",\"en\":\"Lusashogh\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "lusashogh", 168 },
                    { new Guid("6d8a15b8-e379-5f10-bfe4-c6e6f1965ab2"), true, "Village", "{\"hy\":\"\\u0547\\u0561\\u0570\\u0578\\u0582\\u0574\\u0575\\u0561\\u0576\",\"ru\":\"\\u0428\\u0430\\u0443\\u043C\\u044F\\u043D\",\"en\":\"Shahumyan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "shahumyan", 194 },
                    { new Guid("6d9e9d52-f32e-5bfd-bc3d-908955430c86"), true, "Village", "{\"hy\":\"\\u0548\\u057D\\u056F\\u0565\\u057F\\u0561\\u0583\",\"ru\":\"\\u0412\\u043E\\u0441\\u043A\\u0435\\u0442\\u0430\\u043F\",\"en\":\"Vosketap\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vosketap", 196 },
                    { new Guid("6dd1f449-6fa2-5255-a337-ba2f3ace04ee"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\\u0563\\u0561\\u056E\\u0578\\u057F\\u0576\",\"ru\":\"\\u0410\\u0440\\u0430\\u0433\\u0430\\u0446\\u043E\\u0442\\u043D\",\"en\":\"Aragatsotn\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aragatsotn", 18 },
                    { new Guid("6e9ade05-7917-557f-9267-fc99bd7f0c9c"), true, "Village", "{\"hy\":\"\\u0540\\u0561\\u0580\\u0569\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0440\\u0442\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Hartavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "hartavan", 66 },
                    { new Guid("6ffe65c1-f95a-53ff-947e-048a60f1e50c"), true, "Village", "{\"hy\":\"\\u054D\\u0561\\u057D\\u0578\\u0582\\u0576\\u056B\\u056F\",\"ru\":\"\\u0421\\u0430\\u0441\\u0443\\u043D\\u0438\\u043A\",\"en\":\"Sasunik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "sasunik", 104 },
                    { new Guid("70b6c629-6a6f-54e9-9f54-61db25007bdc"), true, "Village", "{\"hy\":\"\\u054D\\u0561\\u0564\\u0578\\u0582\\u0576\\u0581\",\"ru\":\"\\u0421\\u0430\\u0434\\u0443\\u043D\\u0446\",\"en\":\"Sadunts\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "sadunts", 102 },
                    { new Guid("70fde0d4-0327-5816-b101-17e689495931"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0587\\u0578\\u0582\\u057F\",\"ru\":\"\\u0410\\u0440\\u0435\\u0432\\u0443\\u0442\",\"en\":\"Arevut\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "arevut", 22 },
                    { new Guid("714d0e20-3360-542f-b941-ecf7b9ee9f0f"), true, "Village", "{\"hy\":\"\\u0544\\u0561\\u057D\\u057F\\u0561\\u0580\\u0561\",\"ru\":\"\\u041C\\u0430\\u0441\\u0442\\u0430\\u0440\\u0430\",\"en\":\"Mastara\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "mastara", 74 },
                    { new Guid("72111424-f132-5aca-918c-8f19aeb7dc01"), true, "Village", "{\"hy\":\"\\u0531\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0432\\u0430\\u043D\",\"en\":\"Avan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "avan", 13 },
                    { new Guid("727a69f5-b978-5668-bcdf-2913476650cf"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0575\\u0561\\u056F\\u0565\\u0580\\u057F\",\"ru\":\"\\u041D\\u043E\\u044F\\u043A\\u0435\\u0440\\u0442\",\"en\":\"Noyakert\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "noyakert", 186 },
                    { new Guid("73a2ee6f-100d-50c3-8222-9c10c77ffa8a"), true, "City", "{\"hy\":\"\\u0532\\u0575\\u0578\\u0582\\u0580\\u0565\\u0572\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0411\\u044E\\u0440\\u0435\\u0433\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Byureghavan\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "byureghavan", 227 },
                    { new Guid("74736870-23c5-5eb3-b460-c29bcec9b5a6"), true, "Village", "{\"hy\":\"\\u0553\\u0561\\u0580\\u057A\\u056B\",\"ru\":\"\\u041F\\u0430\\u0440\\u043F\\u0438\",\"en\":\"Parpi\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "parpi", 115 },
                    { new Guid("74df2292-d88f-59b0-be1e-8dc2102d1236"), true, "Village", "{\"hy\":\"\\u0540\\u0578\\u057E\\u057F\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041E\\u0432\\u0442\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Hovtashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "hovtashen", 176 },
                    { new Guid("7760d494-7063-53b6-b993-f0b26130481f"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041D\\u043E\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Norashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "norashen-ararat", 193 },
                    { new Guid("78426eac-3c61-536d-9b27-6f2fbee65d96"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041D\\u043E\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Norashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "norashen-aragatsotn", 86 },
                    { new Guid("7a56614e-0e7e-5bd7-86db-a340b0b7218a"), true, "Village", "{\"hy\":\"\\u0532\\u0561\\u0566\\u0574\\u0561\\u0572\\u0562\\u0575\\u0578\\u0582\\u0580\",\"ru\":\"\\u0411\\u0430\\u0437\\u043C\\u0430\\u0445\\u0431\\u044E\\u0440\",\"en\":\"Bazmaghbyur\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "bazmaghbyur", 23 },
                    { new Guid("7a607b40-1aac-5a21-8d31-e681c8bb3518"), true, "Village", "{\"hy\":\"\\u054E\\u0565\\u0580\\u056B\\u0576 \\u0532\\u0561\\u0566\\u0574\\u0561\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u0412\\u0435\\u0440\\u0438\\u043D \\u0411\\u0430\\u0437\\u043C\\u0430\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Verin Bazmaberd\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "verin-bazmaberd", 112 },
                    { new Guid("7a7e50d5-ba7e-5041-b706-70ba74b5b4b3"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0576\\u0561\\u0579\\u0578\\u0582\\u057F\",\"ru\":\"\\u041A\\u0430\\u043D\\u0430\\u0447\\u0443\\u0442\",\"en\":\"Kanachut\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "kanachut", 171 },
                    { new Guid("7fc90b5a-f5c4-550d-8c12-01b879e4beba"), true, "Village", "{\"hy\":\"\\u054E\\u0565\\u0580\\u056B\\u0576 \\u054D\\u0561\\u057D\\u0576\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0412\\u0435\\u0440\\u0438\\u043D \\u0421\\u0430\\u0441\\u043D\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Verin Sasnashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "verin-sasnashen", 113 },
                    { new Guid("82213ff8-477b-54a3-a971-c40eddfe0450"), true, "Village", "{\"hy\":\"\\u0547\\u0565\\u0576\\u056F\\u0561\\u0576\\u056B\",\"ru\":\"\\u0428\\u0435\\u043D\\u043A\\u0430\\u043D\\u0438\",\"en\":\"Shenkani\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "shenkani", 89 },
                    { new Guid("8275648c-9fd3-50de-91d0-8342ae085419"), true, "Village", "{\"hy\":\"\\u054D\\u0561\\u0572\\u0574\\u0578\\u057D\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0421\\u0430\\u0445\\u043C\\u043E\\u0441\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Saghmosavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "saghmosavan", 103 },
                    { new Guid("877f3744-5cf6-5e32-950f-51de2581f81a"), true, "Village", "{\"hy\":\"\\u0541\\u0578\\u0580\\u0561\\u0563\\u056C\\u0578\\u0582\\u056D\",\"ru\":\"\\u0414\\u0437\\u043E\\u0440\\u0430\\u0433\\u043B\\u0443\\u0445\",\"en\":\"Dzoraglukh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dzoraglukh", 70 },
                    { new Guid("88c8c91e-4c13-5747-a733-5e7bd631d2e2"), true, "Village", "{\"hy\":\"\\u0531\\u057E\\u0577\\u0565\\u0576\",\"ru\":\"\\u0410\\u0432\\u0448\\u0435\\u043D\",\"en\":\"Avshen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "avshen", 14 },
                    { new Guid("8939a693-2bf6-586f-aa3a-ca7e2305be3d"), true, "Village", "{\"hy\":\"\\u0542\\u0561\\u0566\\u0561\\u0580\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0425\\u0430\\u0437\\u0430\\u0440\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Ghazaravan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ghazaravan", 72 },
                    { new Guid("89a1ab45-75d3-5526-a126-b5a7876d5a45"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0580\\u0562\\u056B\",\"ru\":\"\\u041A\\u0430\\u0440\\u0431\\u0438\",\"en\":\"Karbi\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "karbi", 60 },
                    { new Guid("89c8f096-3be2-58ca-b68a-a1504e479549"), true, "Village", "{\"hy\":\"\\u054E\\u0565\\u0580\\u056B\\u0576 \\u0531\\u0580\\u057F\\u0561\\u0577\\u0561\\u057F\",\"ru\":\"\\u0412\\u0435\\u0440\\u0438\\u043D \\u0410\\u0440\\u0442\\u0430\\u0448\\u0430\\u0442\",\"en\":\"Verin Artashat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "verin-artashat", 212 },
                    { new Guid("8ab9f556-0a0f-50a9-bafe-2ab9cc829be3"), true, "Village", "{\"hy\":\"\\u0531\\u0572\\u0571\\u0584\",\"ru\":\"\\u0410\\u0445\\u0434\\u0437\\u043A\",\"en\":\"Aghdzk\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aghdzk", 9 },
                    { new Guid("8b78ef24-8d64-550a-8a6e-84e0ea933b00"), true, "Village", "{\"hy\":\"\\u054B\\u0580\\u0561\\u0574\\u0562\\u0561\\u0580\",\"ru\":\"\\u0414\\u0436\\u0440\\u0430\\u043C\\u0431\\u0430\\u0440\",\"en\":\"Jrambar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "jrambar", 100 },
                    { new Guid("8bcdaace-a625-59e4-a61b-d51d470ccda8"), true, "Village", "{\"hy\":\"\\u0547\\u0565\\u0576\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0428\\u0435\\u043D\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Shenavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "shenavan", 88 },
                    { new Guid("8c0c5e47-5489-58c9-a3d0-73c1cc6255e1"), true, "Village", "{\"hy\":\"\\u053C\\u0578\\u0582\\u057D\\u0561\\u057C\\u0561\\u057F\",\"ru\":\"\\u041B\\u0443\\u0441\\u0430\\u0440\\u0430\\u0442\",\"en\":\"Lusarat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "lusarat", 169 },
                    { new Guid("8c842bcf-135f-543d-8f9f-22362180490c"), true, "Village", "{\"hy\":\"\\u0531\\u0575\\u0563\\u0565\\u057D\\u057F\\u0561\\u0576\",\"ru\":\"\\u0410\\u0439\\u0433\\u0435\\u0441\\u0442\\u0430\\u043D\",\"en\":\"Aygestan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "aygestan", 131 },
                    { new Guid("8dff5b61-675b-55cb-90fd-71cfedca5d42"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\\u0563\\u0561\\u056E\",\"ru\":\"\\u0410\\u0440\\u0430\\u0433\\u0430\\u0446\",\"en\":\"Aragats\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "aragats", 16 },
                    { new Guid("900d5fc3-f23c-51a3-83c3-20713de7c24d"), true, "City", "{\"hy\":\"\\u0532\\u0565\\u0580\\u0564\",\"ru\":\"\\u0411\\u0435\\u0440\\u0434\",\"en\":\"Berd\"}", new Guid("019a0000-0000-7000-8000-000000000410"), "berd", 252 },
                    { new Guid("9089d68f-7155-5197-b221-ef15e6a8a8e4"), true, "Village", "{\"hy\":\"\\u0532\\u0578\\u0582\\u0580\\u0561\\u057D\\u057F\\u0561\\u0576\",\"ru\":\"\\u0411\\u0443\\u0440\\u0430\\u0441\\u0442\\u0430\\u043D\",\"en\":\"Burastan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "burastan", 146 },
                    { new Guid("937a3222-8858-5bc9-bf4b-ff840f8e6552"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0574\\u0561\\u0577\",\"ru\":\"\\u0410\\u0440\\u043C\\u0430\\u0448\",\"en\":\"Armash\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "armash", 138 },
                    { new Guid("94347404-c7cb-58e5-8ea7-9348c9a3afd1"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0576\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0412\\u0430\\u043D\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Vanashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vanashen", 209 },
                    { new Guid("95bf00fe-32ed-55f6-ad93-ca788c0fb9a6"), true, "Village", "{\"hy\":\"\\u0540\\u0576\\u0561\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u041D\\u0430\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Hnaberd\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "hnaberd-ararat", 174 },
                    { new Guid("9760cd27-2777-5c01-806b-949dc907971b"), true, "Village", "{\"hy\":\"\\u054D\\u0578\\u0580\\u056B\\u056F\",\"ru\":\"\\u0421\\u043E\\u0440\\u0438\\u043A\",\"en\":\"Sorik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "sorik", 107 },
                    { new Guid("97d83619-bfdf-54aa-9f07-34e818fe440b"), true, "Village", "{\"hy\":\"\\u0536\\u0561\\u0576\\u0563\\u0561\\u056F\\u0561\\u057F\\u0578\\u0582\\u0576\",\"ru\":\"\\u0417\\u0430\\u043D\\u0433\\u0430\\u043A\\u0430\\u0442\\u0443\\u043D\",\"en\":\"Zangakatun\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "zangakatun", 163 },
                    { new Guid("97eb110a-6720-5b5c-94c1-6c7f72dfe543"), true, "Village", "{\"hy\":\"\\u0548\\u057D\\u057F\\u0561\\u0576\",\"ru\":\"\\u0412\\u043E\\u0441\\u0442\\u0430\\u043D\",\"en\":\"Vostan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vostan", 197 },
                    { new Guid("98f65241-8404-59f8-9305-756e2778a95b"), true, "City", "{\"hy\":\"\\u054F\\u0561\\u0577\\u056B\\u0580\",\"ru\":\"\\u0422\\u0430\\u0448\\u0438\\u0440\",\"en\":\"Tashir\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "tashir", 240 },
                    { new Guid("9a167d4e-24f6-5ec9-a90e-c426f414cf08"), true, "Village", "{\"hy\":\"\\u054A\\u0561\\u0580\\u0578\\u0582\\u0575\\u0580 \\u054D\\u0587\\u0561\\u056F\",\"ru\":\"\\u041F\\u0430\\u0440\\u0443\\u0439\\u0440 \\u0421\\u0435\\u0432\\u0430\\u043A\",\"en\":\"Paruyr Sevak\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "paruyr-sevak", 200 },
                    { new Guid("9c679348-37d3-5687-af04-72678096c966"), true, "Village", "{\"hy\":\"\\u054D\\u0578\\u0582\\u0580\\u0565\\u0576\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0421\\u0443\\u0440\\u0435\\u043D\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Surenavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "surenavan", 208 },
                    { new Guid("9fdefe60-a1fa-5d9d-9c95-120e2f49503c"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u0413\\u0435\\u0445\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Geghadzor\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "geghadzor", 28 },
                    { new Guid("a022782b-3594-5608-9634-43184cacea50"), true, "Village", "{\"hy\":\"\\u054B\\u0580\\u0561\\u0570\\u0578\\u057E\\u056B\\u057F\",\"ru\":\"\\u0414\\u0436\\u0440\\u0430\\u043E\\u0432\\u0438\\u0442\",\"en\":\"Jrahovit\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "jrahovit", 201 },
                    { new Guid("a1b7c3f5-46d1-503a-ae4f-ebe5a76e1dc6"), true, "Village", "{\"hy\":\"\\u0555\\u0580\\u0563\\u0578\\u057E\",\"ru\":\"\\u041E\\u0440\\u0433\\u043E\\u0432\",\"en\":\"Orgov\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "orgov", 120 },
                    { new Guid("a46beeae-7e7d-5076-b310-580c938430b6"), true, "City", "{\"hy\":\"\\u0533\\u0575\\u0578\\u0582\\u0574\\u0580\\u056B\",\"ru\":\"\\u0413\\u044E\\u043C\\u0440\\u0438\",\"en\":\"Gyumri\"}", new Guid("019a0000-0000-7000-8000-000000000408"), "gyumri", 242 },
                    { new Guid("a4a1bda1-d6ef-5819-be2d-3360747285f8"), true, "Village", "{\"hy\":\"\\u0540\\u0561\\u056F\\u0578\",\"ru\":\"\\u0410\\u043A\\u043E\",\"en\":\"Hako\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "hako", 65 },
                    { new Guid("a4c0490a-f872-558d-a0db-12eec1806460"), true, "Village", "{\"hy\":\"\\u0549\\u0561\\u0580\\u0579\\u0561\\u056F\\u056B\\u057D\",\"ru\":\"\\u0427\\u0430\\u0440\\u0447\\u0430\\u043A\\u0438\\u0441\",\"en\":\"Charchakis\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "charchakis", 97 },
                    { new Guid("a5207ca4-01ba-5506-8bfe-58557c6d58a6"), true, "City", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0565\\u0576\\u056B\\u057D\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0435\\u043D\\u0438\\u0441\",\"en\":\"Vardenis\"}", new Guid("019a0000-0000-7000-8000-000000000405"), "vardenis", 225 },
                    { new Guid("a56bbab0-adb1-5c6b-9548-1c9fb766567e"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u0577\\u057F\\u0561\\u0564\\u0565\\u0574\",\"ru\":\"\\u0414\\u0430\\u0448\\u0442\\u0430\\u0434\\u0435\\u043C\",\"en\":\"Dashtadem\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dashtadem", 31 },
                    { new Guid("a8949b34-0ee7-5afa-94f4-e9e7da5a7ac4"), true, "Village", "{\"hy\":\"\\u053D\\u0561\\u0579\\u0583\\u0561\\u0580\",\"ru\":\"\\u0425\\u0430\\u0447\\u043F\\u0430\\u0440\",\"en\":\"Khachpar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "khachpar", 170 },
                    { new Guid("a90b8fa7-1f22-5352-b331-da9469603242"), true, "City", "{\"hy\":\"\\u054B\\u0565\\u0580\\u0574\\u0578\\u0582\\u056F\",\"ru\":\"\\u0414\\u0436\\u0435\\u0440\\u043C\\u0443\\u043A\",\"en\":\"Jermuk\"}", new Guid("019a0000-0000-7000-8000-000000000411"), "jermuk", 257 },
                    { new Guid("a932f5f6-bb9a-52c3-b532-c657b8470e96"), true, "Village", "{\"hy\":\"\\u0532\\u0561\\u0572\\u0580\\u0561\\u0574\\u0575\\u0561\\u0576\",\"ru\":\"\\u0411\\u0430\\u0445\\u0440\\u0430\\u043C\\u044F\\u043D\",\"en\":\"Baghramyan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "baghramyan", 141 },
                    { new Guid("abf7e5cb-0b1c-50f2-94fa-d6415daad51a"), true, "Village", "{\"hy\":\"\\u0544\\u0565\\u056C\\u056B\\u0584\\u0563\\u0575\\u0578\\u0582\\u0572\",\"ru\":\"\\u041C\\u0435\\u043B\\u0438\\u043A\\u0433\\u044E\\u0445\",\"en\":\"Melikgyugh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "melikgyugh", 75 },
                    { new Guid("ac35087c-9a86-5be9-89df-79af9edb1b0a"), true, "Village", "{\"hy\":\"\\u054E\\u0565\\u0580\\u056B\\u0576 \\u0534\\u057E\\u056B\\u0576\",\"ru\":\"\\u0412\\u0435\\u0440\\u0438\\u043D \\u0414\\u0432\\u0438\\u043D\",\"en\":\"Verin Dvin\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "verin-dvin", 213 },
                    { new Guid("acb31c57-be91-5d11-a68a-d75358cf7d9d"), true, "Village", "{\"hy\":\"\\u0540\\u0561\\u0575\\u0561\\u0576\\u056B\\u057D\\u057F\",\"ru\":\"\\u0410\\u044F\\u043D\\u0438\\u0441\\u0442\",\"en\":\"Hayanist\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "hayanist", 173 },
                    { new Guid("acf174dd-3bdd-5748-a3b3-ddec80da9992"), true, "Village", "{\"hy\":\"\\u0546\\u0565\\u0580\\u0584\\u056B\\u0576 \\u0532\\u0561\\u0566\\u0574\\u0561\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u041D\\u0435\\u0440\\u043A\\u0438\\u043D \\u0411\\u0430\\u0437\\u043C\\u0430\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Nerkin Bazmaberd\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nerkin-bazmaberd", 79 },
                    { new Guid("ae844dac-df57-524d-93c1-5033909b9a61"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0565\\u0576\\u056B\\u057D\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0435\\u043D\\u0438\\u0441\",\"en\":\"Vardenis\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "vardenis-aragatsotn", 110 },
                    { new Guid("af7846bf-e5d1-5708-9bf2-53c28d9d40c4"), true, "Village", "{\"hy\":\"\\u0531\\u0575\\u0563\\u0565\\u057A\\u0561\\u057F\",\"ru\":\"\\u0410\\u0439\\u0433\\u0435\\u043F\\u0430\\u0442\",\"en\":\"Aygepat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "aygepat", 130 },
                    { new Guid("b00cd8dd-bf1d-5e2c-818a-ffbb2f7cca52"), true, "Village", "{\"hy\":\"\\u0533\\u0578\\u0580\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0413\\u043E\\u0440\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Goravan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "goravan", 151 },
                    { new Guid("b106dc3e-e248-5ab5-8c70-9dd757244d78"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u053F\\u0575\\u0561\\u0576\\u0584\",\"ru\":\"\\u041D\\u043E\\u0440 \\u041A\\u044F\\u043D\\u043A\",\"en\":\"Nor Kyank\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nor-kyank", 188 },
                    { new Guid("b13ff228-88db-59c1-b08a-c9db9f593af3"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u053F\\u0575\\u0578\\u0582\\u0580\\u056B\\u0576\",\"ru\":\"\\u041D\\u043E\\u0440 \\u041A\\u044E\\u0440\\u0438\\u043D\",\"en\":\"Nor Kyurin\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nor-kyurin", 189 },
                    { new Guid("b230d8de-befd-54b7-882a-03a6e7f91590"), true, "City", "{\"hy\":\"\\u0533\\u0561\\u057E\\u0561\\u057C\",\"ru\":\"\\u0413\\u0430\\u0432\\u0430\\u0440\",\"en\":\"Gavar\"}", new Guid("019a0000-0000-7000-8000-000000000405"), "gavar", 221 },
                    { new Guid("b35abee8-0d06-5fbc-80b0-b30c4afc0111"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0565\\u0576\\u0578\\u0582\\u057F\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0435\\u043D\\u0443\\u0442\",\"en\":\"Vardenut\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "vardenut", 111 },
                    { new Guid("b3850ad2-7cbf-500a-8402-1d11a2188580"), true, "Village", "{\"hy\":\"\\u0531\\u0562\\u0578\\u057E\\u0575\\u0561\\u0576\",\"ru\":\"\\u0410\\u0431\\u043E\\u0432\\u044F\\u043D\",\"en\":\"Abovyan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "abovyan-ararat", 125 },
                    { new Guid("b437a401-6270-5525-b256-90d3ced0792e"), true, "City", "{\"hy\":\"\\u0544\\u0561\\u0580\\u0561\\u056C\\u056B\\u056F\",\"ru\":\"\\u041C\\u0430\\u0440\\u0430\\u043B\\u0438\\u043A\",\"en\":\"Maralik\"}", new Guid("019a0000-0000-7000-8000-000000000408"), "maralik", 243 },
                    { new Guid("b55dd564-d323-53c7-bb82-09a0871e7418"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580\\u0561\\u0562\\u0561\\u0581\",\"ru\":\"\\u041D\\u043E\\u0440\\u0430\\u0431\\u0430\\u0446\",\"en\":\"Norabats\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "norabats", 191 },
                    { new Guid("b5637593-6f15-5a45-809a-3adaa8d9eb3b"), true, "City", "{\"hy\":\"\\u0534\\u056B\\u056C\\u056B\\u057B\\u0561\\u0576\",\"ru\":\"\\u0414\\u0438\\u043B\\u0438\\u0436\\u0430\\u043D\",\"en\":\"Dilijan\"}", new Guid("019a0000-0000-7000-8000-000000000410"), "dilijan", 253 },
                    { new Guid("b5a943bb-72c5-555a-8490-3ace00592a56"), true, "Village", "{\"hy\":\"\\u053C\\u0561\\u0576\\u057B\\u0561\\u0576\\u056B\\u057D\\u057F\",\"ru\":\"\\u041B\\u0430\\u043D\\u0434\\u0436\\u0430\\u043D\\u0438\\u0441\\u0442\",\"en\":\"Lanjanist\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "lanjanist", 166 },
                    { new Guid("b67800a9-15e2-5c96-9eec-12e9587d356b"), true, "Village", "{\"hy\":\"\\u0535\\u0580\\u0576\\u057B\\u0561\\u057F\\u0561\\u0583\",\"ru\":\"\\u0415\\u0440\\u043D\\u0434\\u0436\\u0430\\u0442\\u0430\\u043F\",\"en\":\"Yernjatap\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "yernjatap", 37 },
                    { new Guid("b7226c93-0337-5104-967f-a4259d74cd94"), true, "City", "{\"hy\":\"\\u054D\\u0587\\u0561\\u0576\",\"ru\":\"\\u0421\\u0435\\u0432\\u0430\\u043D\",\"en\":\"Sevan\"}", new Guid("019a0000-0000-7000-8000-000000000405"), "sevan", 224 },
                    { new Guid("b7849a08-a31b-5717-b262-756d66337ff6"), true, "Village", "{\"hy\":\"\\u0531\\u057A\\u0576\\u0561\\u0563\\u0575\\u0578\\u0582\\u0572\",\"ru\":\"\\u0410\\u043F\\u043D\\u0430\\u0433\\u044E\\u0445\",\"en\":\"Apnagyugh\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "apnagyugh", 12 },
                    { new Guid("b7b96c25-e952-5326-a0ba-1d70cb18f3a0"), true, "Village", "{\"hy\":\"\\u0536\\u0561\\u0580\\u056B\\u0576\\u057B\\u0561\",\"ru\":\"\\u0417\\u0430\\u0440\\u0438\\u043D\\u0434\\u0436\\u0430\",\"en\":\"Zarinja\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "zarinja", 38 },
                    { new Guid("b7c6049a-3ab9-574c-aefa-22da889b5191"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u057E\\u0569\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0414\\u0430\\u0432\\u0442\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Davtashen\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "davtashen", 32 },
                    { new Guid("b955f94a-1809-5ad0-a82b-8f1080b021ab"), true, "Village", "{\"hy\":\"\\u0531\\u0576\\u057F\\u0561\\u057C\\u0578\\u0582\\u057F\",\"ru\":\"\\u0410\\u043D\\u0442\\u0430\\u0440\\u0443\\u0442\",\"en\":\"Antarut\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "antarut", 10 },
                    { new Guid("ba07e277-0da8-5bf6-9023-8bfed1f059c3"), true, "Village", "{\"hy\":\"\\u0534\\u057A\\u0580\\u0565\\u057E\\u0561\\u0576\\u0584\",\"ru\":\"\\u0414\\u043F\\u0440\\u0435\\u0432\\u0430\\u043D\\u043A\",\"en\":\"Dprevank\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dprevank", 34 },
                    { new Guid("bc13d566-ae47-53ec-ace1-fc53a17c803b"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u0572\\u0561\\u0576\\u056B\\u057D\\u057F\",\"ru\":\"\\u0413\\u0435\\u0445\\u0430\\u043D\\u0438\\u0441\\u0442\",\"en\":\"Geghanist\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "geghanist", 147 },
                    { new Guid("bcef089d-2145-5fa5-ba99-a1c1c8d69775"), true, "Village", "{\"hy\":\"\\u0539\\u0565\\u0572\\u0565\\u0580\",\"ru\":\"\\u0422\\u0435\\u0445\\u0435\\u0440\",\"en\":\"Tegher\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tegher", 41 },
                    { new Guid("be3f5d5e-4cbd-54b7-ad0f-cb2627f00bfa"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0548\\u0582\\u0572\\u056B\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0423\\u0445\\u0438\",\"en\":\"Nor Ughi\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nor-ughi", 190 },
                    { new Guid("bfe3ed7e-fb20-5c07-98bd-549993105f4a"), true, "Village", "{\"hy\":\"\\u054C\\u0561\\u0576\\u0579\\u057A\\u0561\\u0580\",\"ru\":\"\\u0420\\u0430\\u043D\\u0447\\u043F\\u0430\\u0440\",\"en\":\"Ranchpar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ranchpar", 203 },
                    { new Guid("c18dd1fd-bf63-54d4-bdd9-4ec1b33dcf7d"), true, "Village", "{\"hy\":\"\\u0547\\u0561\\u0574\\u056B\\u0580\\u0561\\u0574\",\"ru\":\"\\u0428\\u0430\\u043C\\u0438\\u0440\\u0430\\u043C\",\"en\":\"Shamiram\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "shamiram", 87 },
                    { new Guid("c2580a23-f796-58b2-ba4a-a004e9dac29b"), true, "City", "{\"hy\":\"\\u0549\\u0561\\u0580\\u0565\\u0576\\u0581\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0427\\u0430\\u0440\\u0435\\u043D\\u0446\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Charentsavan\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "charentsavan", 232 },
                    { new Guid("c29c3e7d-e723-5e00-806c-f05c02083ac7"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0587\\u0577\\u0561\\u057F\",\"ru\":\"\\u0410\\u0440\\u0435\\u0432\\u0448\\u0430\\u0442\",\"en\":\"Arevshat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "arevshat", 140 },
                    { new Guid("c2d33bea-ffdf-50f6-b20b-bdb8e482bc71"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0580\\u056B\\u0576\",\"ru\":\"\\u041A\\u0430\\u0440\\u0438\\u043D\",\"en\":\"Karin\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "karin", 61 },
                    { new Guid("c8d781c5-4893-5d08-9645-498d709c4d0c"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u0577\\u057F\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0414\\u0430\\u0448\\u0442\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Dashtavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "dashtavan", 153 },
                    { new Guid("c9a1eb90-fb86-527e-974e-22cf238ec5bb"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580\\u0561\\u0574\\u0561\\u0580\\u0563\",\"ru\":\"\\u041D\\u043E\\u0440\\u0430\\u043C\\u0430\\u0440\\u0433\",\"en\":\"Noramarg\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "noramarg", 192 },
                    { new Guid("cc24dee6-cbe5-58a6-a921-b0a793f48344"), true, "Village", "{\"hy\":\"\\u0531\\u0563\\u0561\\u0580\\u0561\\u056F\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0433\\u0430\\u0440\\u0430\\u043A\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Agarakavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "agarakavan", 6 },
                    { new Guid("ccf98ec0-50c4-5452-9913-4b11d32aac1c"), true, "Village", "{\"hy\":\"\\u054D\\u056B\\u057D\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0421\\u0438\\u0441\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Sisavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "sisavan", 206 },
                    { new Guid("ce6877bb-cba2-5966-9cea-26ef8f6b27c1"), true, "Village", "{\"hy\":\"\\u0541\\u0574\\u0576\\u0561\\u057D\\u0561\\u0580\",\"ru\":\"\\u0414\\u0437\\u043C\\u043D\\u0430\\u0441\\u0430\\u0440\",\"en\":\"Dzmnasar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "dzmnasar", 69 },
                    { new Guid("cf4b0ec9-4559-543b-82f8-de38dc0fd41d"), true, "City", "{\"hy\":\"\\u053E\\u0561\\u0572\\u056F\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u0426\\u0430\\u0445\\u043A\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Tsaghkadzor\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "tsaghkadzor", 229 },
                    { new Guid("d00c6fa6-049c-502a-804c-4e25a9e45fac"), true, "Village", "{\"hy\":\"\\u054C\\u0575\\u0561 \\u0539\\u0561\\u0566\\u0561\",\"ru\":\"\\u0420\\u044F \\u0422\\u0430\\u0437\\u0430\",\"en\":\"Rya Taza\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "rya-taza", 101 },
                    { new Guid("d08240f6-ba2c-5ad8-a716-505c87bffdca"), true, "Village", "{\"hy\":\"\\u0547\\u0561\\u0572\\u0561\\u0583\",\"ru\":\"\\u0428\\u0430\\u0445\\u0430\\u043F\",\"en\":\"Shaghap\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "shaghap", 195 },
                    { new Guid("d0dace92-bfc7-56c9-9238-371157a51409"), true, "Village", "{\"hy\":\"\\u0548\\u0582\\u057B\\u0561\\u0576\",\"ru\":\"\\u0423\\u0434\\u0436\\u0430\\u043D\",\"en\":\"Ujan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ujan", 96 },
                    { new Guid("d211e6d8-e1ae-54cc-881b-24d499b108ec"), true, "Village", "{\"hy\":\"\\u0542\\u0578\\u0582\\u056F\\u0561\\u057D\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0425\\u0443\\u043A\\u0430\\u0441\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Ghukasavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ghukasavan", 177 },
                    { new Guid("d2752cfe-6c6b-5f93-b704-e43badeac850"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0584\\u0561\\u057E\\u0561\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u041A\\u0430\\u043A\\u0430\\u0432\\u0430\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Kakavaberd\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "kakavaberd", 172 },
                    { new Guid("d277d5f3-3119-5de2-b057-0c24a9a923d6"), true, "Village", "{\"hy\":\"\\u053C\\u0561\\u0576\\u057B\\u0561\\u0566\\u0561\\u057F\",\"ru\":\"\\u041B\\u0430\\u043D\\u0434\\u0436\\u0430\\u0437\\u0430\\u0442\",\"en\":\"Lanjazat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "lanjazat", 165 },
                    { new Guid("d39363f6-6134-548b-bc66-2d0e1a1e0f2a"), true, "Village", "{\"hy\":\"\\u053B\\u0580\\u056B\\u0576\\u0564\",\"ru\":\"\\u0418\\u0440\\u0438\\u043D\\u0434\",\"en\":\"Irind\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "irind", 44 },
                    { new Guid("d3e3ac72-c8f3-5aa8-a854-1f0be4405469"), true, "Village", "{\"hy\":\"\\u0531\\u057E\\u0577\\u0561\\u0580\",\"ru\":\"\\u0410\\u0432\\u0448\\u0430\\u0440\",\"en\":\"Avshar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "avshar", 133 },
                    { new Guid("d6f666f3-ba1d-5e5a-b3a2-9c65291d45a3"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\",\"ru\":\"\\u0410\\u0440\\u0430\",\"en\":\"Ara\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "ara", 15 },
                    { new Guid("d888beef-abf2-534a-b8ff-4a5575883334"), true, "City", "{\"hy\":\"\\u0531\\u0580\\u0574\\u0561\\u057E\\u056B\\u0580\",\"ru\":\"\\u0410\\u0440\\u043C\\u0430\\u0432\\u0438\\u0440\",\"en\":\"Armavir\"}", new Guid("019a0000-0000-7000-8000-000000000404"), "armavir", 218 },
                    { new Guid("d8f7aaa1-ddf9-5ac2-b07f-c9b8b8454f87"), true, "Village", "{\"hy\":\"\\u0536\\u0578\\u057E\\u0561\\u057D\\u0561\\u0580\",\"ru\":\"\\u0417\\u043E\\u0432\\u0430\\u0441\\u0430\\u0440\",\"en\":\"Zovasar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "zovasar", 39 },
                    { new Guid("da18f8ae-8282-5c53-944b-a7569493d60a"), true, "Village", "{\"hy\":\"\\u0555\\u0569\\u0587\\u0561\\u0576\",\"ru\":\"\\u041E\\u0442\\u0435\\u0432\\u0430\\u043D\",\"en\":\"Otevan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "otevan", 117 },
                    { new Guid("dc202fef-6d9f-57b9-b979-c7a5e258c552"), true, "City", "{\"hy\":\"\\u0544\\u0565\\u0572\\u0580\\u056B\",\"ru\":\"\\u041C\\u0435\\u0433\\u0440\\u0438\",\"en\":\"Meghri\"}", new Guid("019a0000-0000-7000-8000-000000000409"), "meghri", 248 },
                    { new Guid("dc538040-4f87-5fa6-805a-749a07bb1c31"), true, "City", "{\"hy\":\"\\u0535\\u0572\\u057E\\u0561\\u0580\\u0564\",\"ru\":\"\\u0415\\u0433\\u0432\\u0430\\u0440\\u0434\",\"en\":\"Yeghvard\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "yeghvard", 228 },
                    { new Guid("dc87c9e7-e608-5c94-ab7e-ebceb3487f73"), true, "Village", "{\"hy\":\"\\u054B\\u0561\\u0574\\u0577\\u056C\\u0578\\u0582\",\"ru\":\"\\u0414\\u0436\\u0430\\u043C\\u0448\\u043B\\u0443\",\"en\":\"Jamshlu\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "jamshlu", 99 },
                    { new Guid("dffefe45-4628-5aa1-944e-f514f4d43e2f"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0576\\u0579\",\"ru\":\"\\u041A\\u0430\\u043D\\u0447\",\"en\":\"Kanch\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kanch", 59 },
                    { new Guid("e16520c2-b078-50b4-8e4a-14adc0ad4f09"), true, "City", "{\"hy\":\"\\u0544\\u0561\\u0580\\u057F\\u0578\\u0582\\u0576\\u056B\",\"ru\":\"\\u041C\\u0430\\u0440\\u0442\\u0443\\u043D\\u0438\",\"en\":\"Martuni\"}", new Guid("019a0000-0000-7000-8000-000000000405"), "martuni", 223 },
                    { new Guid("e1a462be-7f03-5032-8c45-14a330a27e65"), true, "Village", "{\"hy\":\"\\u0554\\u0561\\u0572\\u0581\\u0580\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u041A\\u0430\\u0445\\u0446\\u0440\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Kaghtsrashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "kaghtsrashen", 217 },
                    { new Guid("e23bd40f-b6b1-5d73-a4ac-24b9db82f71e"), true, "City", "{\"hy\":\"\\u0531\\u0580\\u0561\\u0580\\u0561\\u057F\",\"ru\":\"\\u0410\\u0440\\u0430\\u0440\\u0430\\u0442\",\"en\":\"Ararat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ararat", 121 },
                    { new Guid("e2c896e5-703b-5d02-a1b9-8b5b627e3fe1"), true, "Village", "{\"hy\":\"\\u0548\\u057D\\u056F\\u0565\\u057E\\u0561\\u0566\",\"ru\":\"\\u0412\\u043E\\u0441\\u043A\\u0435\\u0432\\u0430\\u0437\",\"en\":\"Voskevaz\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "voskevaz", 94 },
                    { new Guid("e333da14-ce9a-50c2-a189-be5fac17280f"), true, "Village", "{\"hy\":\"\\u0548\\u057D\\u056F\\u0565\\u0569\\u0561\\u057D\",\"ru\":\"\\u0412\\u043E\\u0441\\u043A\\u0435\\u0442\\u0430\\u0441\",\"en\":\"Vosketas\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "vosketas", 92 },
                    { new Guid("e3610d1e-93a5-5ece-8fd1-1f9815c36047"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0531\\u0574\\u0561\\u0576\\u0578\\u057D\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0410\\u043C\\u0430\\u043D\\u043E\\u0441\",\"en\":\"Nor Amanos\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nor-amanos", 83 },
                    { new Guid("e4744294-48a0-5f0b-ab73-aa7998911bfa"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u0561\\u0584\\u057D\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0440\\u0430\\u043A\\u0441\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Araksavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "araksavan", 135 },
                    { new Guid("e53c4c04-c3ae-5dc5-a81a-314046bca655"), true, "Village", "{\"hy\":\"\\u053E\\u0561\\u0572\\u056F\\u0561\\u057D\\u0561\\u0580\",\"ru\":\"\\u0426\\u0430\\u0445\\u043A\\u0430\\u0441\\u0430\\u0440\",\"en\":\"Tsaghkasar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tsaghkasar", 53 },
                    { new Guid("e56b37ec-5248-5e33-8a8f-7df8b9da99e1"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0535\\u0564\\u0565\\u057D\\u056B\\u0561\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0415\\u0434\\u0435\\u0441\\u0438\\u0430\",\"en\":\"Nor Yedesia\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nor-yedesia", 85 },
                    { new Guid("e620f5bb-a75a-5060-916a-51a9bb418115"), true, "Village", "{\"hy\":\"\\u0531\\u056F\\u0578\\u0582\\u0576\\u0584\",\"ru\":\"\\u0410\\u043A\\u0443\\u043D\\u043A\",\"en\":\"Akunk\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "akunk", 8 },
                    { new Guid("e6cd397f-57db-568a-ad50-85db428e3383"), true, "City", "{\"hy\":\"\\u0531\\u056D\\u0569\\u0561\\u056C\\u0561\",\"ru\":\"\\u0410\\u0445\\u0442\\u0430\\u043B\\u0430\",\"en\":\"Akhtala\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "akhtala", 234 },
                    { new Guid("e74ed8bb-74eb-5d66-8f42-87bbd371d17a"), true, "Village", "{\"hy\":\"\\u0531\\u0580\\u057F\\u0561\\u0577\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0410\\u0440\\u0442\\u0430\\u0448\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Artashavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "artashavan", 20 },
                    { new Guid("e7e1a574-5ede-5560-b798-f07e9a0dd229"), true, "Village", "{\"hy\":\"\\u0533\\u056B\\u0576\\u0565\\u057E\\u0565\\u057F\",\"ru\":\"\\u0413\\u0438\\u043D\\u0435\\u0432\\u0435\\u0442\",\"en\":\"Ginevet\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ginevet", 150 },
                    { new Guid("e8101a0a-c031-5be4-9d92-2071f12b149e"), true, "City", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u0540\\u0561\\u0573\\u0576\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0410\\u0447\\u0438\\u043D\",\"en\":\"Nor Hachn\"}", new Guid("019a0000-0000-7000-8000-000000000406"), "nor-hachn", 231 },
                    { new Guid("e892b7e1-37f2-5e3b-a03a-9f1b468dea1f"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u056C\\u0561\\u0580\",\"ru\":\"\\u0414\\u0430\\u043B\\u0430\\u0440\",\"en\":\"Dalar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "dalar", 152 },
                    { new Guid("e9f10d1d-062a-533f-af15-369bde010fe1"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0575\\u0584\",\"ru\":\"\\u041A\\u0430\\u0439\\u043A\",\"en\":\"Kayk\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kayk", 57 },
                    { new Guid("eaefae52-3f16-56e2-a045-f292170a59cd"), true, "Village", "{\"hy\":\"\\u054F\\u056B\\u0563\\u0580\\u0561\\u0576\\u0561\\u0577\\u0565\\u0576\",\"ru\":\"\\u0422\\u0438\\u0433\\u0440\\u0430\\u043D\\u0430\\u0448\\u0435\\u043D\",\"en\":\"Tigranashen\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "tigranashen", 215 },
                    { new Guid("ebc2e3c0-1d34-5e60-8563-35618c9bc9c5"), true, "Village", "{\"hy\":\"\\u054E\\u0561\\u0580\\u0564\\u0561\\u0577\\u0561\\u057F\",\"ru\":\"\\u0412\\u0430\\u0440\\u0434\\u0430\\u0448\\u0430\\u0442\",\"en\":\"Vardashat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "vardashat", 210 },
                    { new Guid("ed6bb1ec-1360-5c1e-8f27-68bdbaf815b4"), true, "Village", "{\"hy\":\"\\u0546\\u0578\\u0580 \\u053D\\u0561\\u0580\\u0562\\u0565\\u0580\\u0564\",\"ru\":\"\\u041D\\u043E\\u0440 \\u0425\\u0430\\u0440\\u0431\\u0435\\u0440\\u0434\",\"en\":\"Nor Kharberd\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "nor-kharberd", 187 },
                    { new Guid("ed970a59-d03a-5e03-aed6-d204770b9102"), true, "Village", "{\"hy\":\"\\u0535\\u0572\\u0576\\u056B\\u056F\",\"ru\":\"\\u0415\\u0445\\u043D\\u0438\\u043A\",\"en\":\"Yeghnik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "yeghnik", 36 },
                    { new Guid("ef831d78-fdba-5928-ab42-866dd286a5a7"), true, "Village", "{\"hy\":\"\\u0544\\u056D\\u0579\\u0575\\u0561\\u0576\",\"ru\":\"\\u041C\\u0445\\u0447\\u044F\\u043D\",\"en\":\"Mkhchyan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "mkhchyan", 179 },
                    { new Guid("f1985998-5463-5a70-aee4-36ed2aea1821"), true, "Village", "{\"hy\":\"\\u0534\\u0561\\u0577\\u057F\\u0561\\u0584\\u0561\\u0580\",\"ru\":\"\\u0414\\u0430\\u0448\\u0442\\u0430\\u043A\\u0430\\u0440\",\"en\":\"Dashtakar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "dashtakar", 154 },
                    { new Guid("f1a3b9bd-f023-5279-b92d-9533a53a338f"), true, "Village", "{\"hy\":\"\\u053F\\u0561\\u0584\\u0561\\u057E\\u0561\\u0571\\u0578\\u0580\",\"ru\":\"\\u041A\\u0430\\u043A\\u0430\\u0432\\u0430\\u0434\\u0437\\u043E\\u0440\",\"en\":\"Kakavadzor\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "kakavadzor", 63 },
                    { new Guid("f253c8b0-e0c9-5745-bf72-65c807dfb04a"), true, "Village", "{\"hy\":\"\\u0540\\u0578\\u057E\\u057F\\u0561\\u0577\\u0561\\u057F\",\"ru\":\"\\u041E\\u0432\\u0442\\u0430\\u0448\\u0430\\u0442\",\"en\":\"Hovtashat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "hovtashat", 175 },
                    { new Guid("f2c2a045-6283-50f6-a53f-5a1e4abb439f"), true, "Village", "{\"hy\":\"\\u053E\\u056B\\u056C\\u0584\\u0561\\u0580\",\"ru\":\"\\u0426\\u0438\\u043B\\u043A\\u0430\\u0440\",\"en\":\"Tsilkar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tsilkar", 55 },
                    { new Guid("f417ce96-028d-51f8-8e52-b607ee2f3d5b"), true, "Village", "{\"hy\":\"\\u0535\\u0580\\u0561\\u057D\\u056D\",\"ru\":\"\\u0415\\u0440\\u0430\\u0441\\u0445\",\"en\":\"Yeraskh\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "yeraskh", 162 },
                    { new Guid("f793ae57-5fce-5b0d-be48-674c55d19e33"), true, "Village", "{\"hy\":\"\\u0546\\u056B\\u0563\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u041D\\u0438\\u0433\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Nigavan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "nigavan", 81 },
                    { new Guid("f810885c-6da7-5120-94d3-cd7e3d6cc548"), true, "Village", "{\"hy\":\"\\u053E\\u0561\\u0574\\u0561\\u0584\\u0561\\u057D\\u0561\\u0580\",\"ru\":\"\\u0426\\u0430\\u043C\\u0430\\u043A\\u0430\\u0441\\u0430\\u0440\",\"en\":\"Tsamakasar\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "tsamakasar", 54 },
                    { new Guid("f9c0face-ba78-5c57-8d32-46de0bf807db"), true, "City", "{\"hy\":\"\\u0531\\u056C\\u0561\\u057E\\u0565\\u0580\\u0564\\u056B\",\"ru\":\"\\u0410\\u043B\\u0430\\u0432\\u0435\\u0440\\u0434\\u0438\",\"en\":\"Alaverdi\"}", new Guid("019a0000-0000-7000-8000-000000000407"), "alaverdi", 233 },
                    { new Guid("fad55d0c-4e18-5a47-b838-fc556ce93915"), true, "Village", "{\"hy\":\"\\u054E\\u0565\\u0580\\u056B\\u0576 \\u054D\\u0561\\u057D\\u0578\\u0582\\u0576\\u056B\\u056F\",\"ru\":\"\\u0412\\u0435\\u0440\\u0438\\u043D \\u0421\\u0430\\u0441\\u0443\\u043D\\u0438\\u043A\",\"en\":\"Verin Sasunik\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "verin-sasunik", 114 },
                    { new Guid("fb24dac1-d4ca-5463-9a03-78704d821529"), true, "Village", "{\"hy\":\"\\u053C\\u0561\\u0576\\u057B\\u0561\\u057C\",\"ru\":\"\\u041B\\u0430\\u043D\\u0434\\u0436\\u0430\\u0440\",\"en\":\"Lanjar\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "lanjar", 167 },
                    { new Guid("fd28b13c-b141-5b89-8247-afabb3a731cd"), true, "Village", "{\"hy\":\"\\u0533\\u0565\\u057F\\u0561\\u0566\\u0561\\u057F\",\"ru\":\"\\u0413\\u0435\\u0442\\u0430\\u0437\\u0430\\u0442\",\"en\":\"Getazat\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "getazat", 148 },
                    { new Guid("feb9e0ee-25cc-501a-8afa-72279a60298c"), true, "Village", "{\"hy\":\"\\u0548\\u057D\\u056F\\u0565\\u0570\\u0561\\u057F\",\"ru\":\"\\u0412\\u043E\\u0441\\u043A\\u0435\\u0430\\u0442\",\"en\":\"Voskehat\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "voskehat", 93 },
                    { new Guid("ff4117a5-5d26-5774-ba41-7013f426389a"), true, "Village", "{\"hy\":\"\\u054D\\u056B\\u057A\\u0561\\u0576\",\"ru\":\"\\u0421\\u0438\\u043F\\u0430\\u043D\",\"en\":\"Sipan\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "sipan", 106 },
                    { new Guid("ff55e663-bc03-5dbc-aad5-32cffd012f5d"), true, "Village", "{\"hy\":\"\\u0535\\u0572\\u056B\\u057A\\u0561\\u057F\\u0580\\u0578\\u0582\\u0577\",\"ru\":\"\\u0415\\u0445\\u0438\\u043F\\u0430\\u0442\\u0440\\u0443\\u0448\",\"en\":\"Yeghipatrush\"}", new Guid("019a0000-0000-7000-8000-000000000402"), "yeghipatrush", 35 },
                    { new Guid("ff56a4be-78a7-5f9a-b50f-357dd086751b"), true, "Village", "{\"hy\":\"\\u0535\\u0572\\u0565\\u0563\\u0576\\u0561\\u057E\\u0561\\u0576\",\"ru\":\"\\u0415\\u0445\\u0435\\u0433\\u043D\\u0430\\u0432\\u0430\\u043D\",\"en\":\"Yeghegnavan\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "yeghegnavan", 161 },
                    { new Guid("ffbd3c44-1e5d-51f9-a4e4-c7bc3a1ce3fb"), true, "Village", "{\"hy\":\"\\u0536\\u0578\\u0580\\u0561\\u056F\",\"ru\":\"\\u0417\\u043E\\u0440\\u0430\\u043A\",\"en\":\"Zorak\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "zorak", 164 },
                    { new Guid("ffd6832b-2d22-51c3-90ad-e2f7a5bf6cbe"), true, "Village", "{\"hy\":\"\\u0531\\u0575\\u0576\\u0569\\u0561\\u057A\",\"ru\":\"\\u0410\\u0439\\u043D\\u0442\\u0430\\u043F\",\"en\":\"Ayntap\"}", new Guid("019a0000-0000-7000-8000-000000000403"), "ayntap", 132 }
                });

            migrationBuilder.CreateIndex(
                name: "ix_partner_areas_partner_profile_id_region_id_city_id_district~",
                table: "partner_areas",
                columns: new[] { "partner_profile_id", "region_id", "city_id", "district_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_partner_areas_region_id",
                table: "partner_areas",
                column: "region_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_partner_areas_region_or_city",
                table: "partner_areas",
                sql: "(region_id IS NOT NULL AND city_id IS NULL AND district_id IS NULL) OR (region_id IS NULL AND city_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_cities_region_id",
                table: "cities",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_regions_slug",
                table: "regions",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_cities_regions_region_id",
                table: "cities",
                column: "region_id",
                principalTable: "regions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_partner_areas_regions_region_id",
                table: "partner_areas",
                column: "region_id",
                principalTable: "regions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cities_regions_region_id",
                table: "cities");

            migrationBuilder.DropForeignKey(
                name: "fk_partner_areas_regions_region_id",
                table: "partner_areas");

            migrationBuilder.DropTable(
                name: "regions");

            migrationBuilder.DropIndex(
                name: "ix_partner_areas_partner_profile_id_region_id_city_id_district~",
                table: "partner_areas");

            migrationBuilder.DropIndex(
                name: "ix_partner_areas_region_id",
                table: "partner_areas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_partner_areas_region_or_city",
                table: "partner_areas");

            migrationBuilder.DropIndex(
                name: "ix_cities_region_id",
                table: "cities");

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("00a4465c-b1c0-5d60-93ab-a7b20355bf3c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("014ff9d7-3df4-56a7-9fff-48dba75ce2a4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019bcce5-73c2-5550-ab3a-e261255e9cb9"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("02236d1b-d32e-55ae-a8f0-ed4249d26d9e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("02fab6c0-4667-5da5-bc11-864688661902"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0428c381-46ce-5953-902f-fbcfc7e3b6ca"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("05efe3e1-334d-57fb-81fb-e0666607ddf8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("060965dc-4c46-5ea9-8c86-7939cbbc6865"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("07606e42-749c-57ba-bfdf-85ca6117d229"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("07708016-2e87-5696-8a9e-2527698974d0"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("07ab13ad-30a1-5535-8001-b6d5e85663f1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("07b3a0f6-d8cb-5787-9af7-7888da49cb58"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("07cba34c-db44-57c0-a1d3-8319a83a57f5"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0bca866e-ade6-5ed7-9232-4cc3d8328b21"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0c2cbad2-15eb-5e31-9a62-5cb3f3675c8c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0dcb91eb-7941-57d9-931c-484483083bda"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0e63faa9-a808-5d84-903c-28675f0d034d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0e82831b-8cad-5013-b222-29bcd6db6042"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("0fab38a8-3a35-5f3d-bb07-ea6f66d050fe"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("10b7368c-32d3-52e7-ba09-3202057180a5"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("119bbbe1-2485-539c-a443-13a11b495457"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("12433fe9-5c31-5c3d-84bd-70ea0dda8d8e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("1294344f-0208-5d79-b40b-a9ca91089523"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("13039b50-9eac-5737-a9e6-3e36a43db088"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("139b0843-40f6-5557-b7e8-c33d654daf05"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("143554fd-dd6d-58d6-baf6-e3bd2be6ccd0"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("15532b03-34ea-5a39-b751-ddfbf590fea2"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("16d4a451-6ce2-5097-a97d-fff1300c4a63"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("18140e93-b939-59ee-b338-e8d6a95ace1c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("19ead0d3-3df0-5354-b750-c1d6df26bf60"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("1b42820f-d057-53fd-b2b1-746cd6cd4501"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("1bc25562-470d-5790-bb46-198b469b0d14"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("20a7119f-f3a4-56c3-bcbf-fb1522c9f3ae"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("23a46024-6cc5-58c0-accb-4e12193f0599"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2469896b-1b9d-55e6-98eb-c43472748a92"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("24e77f68-00c5-5c93-b883-0f7e3ee4e65e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("261a6566-81e4-5290-80e9-df86ebe7e2c5"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("26f709ea-0180-54d7-9ee7-6408660ed0d1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2843cf5f-2d88-5d92-b18e-74d4ffc5dbf9"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2a097690-9eae-54ae-a414-360f4dd8524b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2a818d67-01b8-5c52-9957-86133aa46852"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2c16b3fa-38b5-5207-9433-d59a7251c325"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2c18fbaa-9d14-5d51-bf31-63bf5f02d28a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2c24c769-cd09-51cf-ad8f-5fb02987ccd2"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2cf43c58-48a6-541d-8242-ae9812e92e07"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2dca1947-a8fd-5511-8483-851a3a8dfe60"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2de143cd-6148-574d-b9e0-99caded10b90"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2e02a995-f5a0-5367-864c-5accc9db9b59"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("2e9115de-2372-58fa-811c-97a4934ce2f8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3032c355-d15b-5ec6-81f2-89eaa6907569"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3044dd4d-bbf7-5a69-ad84-d309b8b210b1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("32a8a66f-48ee-51d3-9155-b8642edd77e6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("34c8cdd2-34be-5dfd-a358-9f75d0c27ddd"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3683e64d-01e7-5ebe-99d0-e615070acd68"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("36ef1397-37ee-5542-b162-07691c2c8faf"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("39b551ae-28f7-5ec1-978e-31b20e088bd5"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3b31dd71-c38e-553e-b78f-c1ba4e752a7c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3b4b58ef-dcf4-5ed6-b1e9-b24d95b44dc3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3cf4f891-5e18-5e43-8361-e88ec9cc6089"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3dbad1d2-721f-5d77-acb9-c35a6690d998"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3ee46a93-cfde-5bf3-a599-9e74dcdacab2"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("3fd81473-cccb-5fba-a8db-53c472b69b8e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("41dda536-f6c7-5dab-b5ca-2febca2debb3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("42070bf9-87b2-54c2-89c7-f673703dcdec"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("421bac50-c125-5533-af1a-0cc87eb4317f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("42bbe1d0-4214-5301-9635-df6d95b63379"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("42dc5b29-fe91-5608-93d1-712630751789"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("433e9a13-d92f-5d17-a729-ac5addb0675a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("43d84ec9-40bc-5072-bf7a-ba5fa2b86a54"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("44ffdce8-337d-5ea3-8e83-4ff4b25c362c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("482fd2f5-55c7-5599-a463-ecf3849aeaa6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("4893ee72-6946-513c-a167-395b097a5cc8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("4a59cd69-32b8-51c8-a8d7-d7e96c90f884"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("4c7e11dd-4cf9-531e-8339-8187a91437d8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("4d3109eb-c4e1-5bb2-879b-37653dab7703"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("4fe1f71e-a19a-540c-bb6f-37eac034ba00"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5252557a-2ad4-52f7-a317-a1f6e6c09d8a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5286d114-5a60-5bda-80f6-cfaef43ad2ff"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("538d736d-c261-5f67-bc9f-85f5123fa10c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5434bca5-2cf9-5a14-87fd-03712fb824bd"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("550a4281-1cdc-5f12-b698-a92dfd546e99"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("553363a1-757e-5caf-9525-f860c50da5ae"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("58246618-fb31-53d0-9dfc-dd732eff87cc"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5854d40c-bded-5bbb-9662-c78d39526851"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("58aaa51a-a682-5689-bc69-9a98f10ccb17"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("59b15bcd-fd94-59e2-83c0-2974d4da2065"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5b110bc4-c0de-50f3-b0ac-3c94b46140b8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5b1be49e-9bb1-5464-b3ad-5c86e1b60636"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5bcba987-399c-5b51-b59c-af58df0bb800"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5cd595cb-8e10-5429-8ecb-0908b3147f73"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5cd71c59-e2a2-55b1-bec7-fb737271927b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5e8030dc-5803-5c34-82d9-092ae62638d9"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5f1ad1ae-45b8-5472-8686-7e407a39a67c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("5fb4c60f-795d-5715-b45c-6b57e8534741"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("60b576ee-f510-5b5b-9d79-a6626df3a15b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("60f91c32-b650-538c-91d8-2cf37bbfc46c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("62d39bee-555b-53b1-a5f0-a4948317cd13"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("64368665-30c1-5d35-b20e-2003d3a402a3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("64f9be27-773e-57f6-9811-b84fe0d5ed3a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("651e1636-6c21-547b-9cb6-6518f93ff173"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6677cc07-aeae-508e-8164-03e21e9b785e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("671d4e99-e9e9-56b3-8ae2-52406d463860"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6900aa5a-d35f-55e1-a34d-35c709714043"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("69a17692-8245-5071-8c18-13767991c563"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6ad91b38-02a5-5325-afd6-9d8d317e82cd"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6aebb208-6c1c-5813-a8e8-0a21c4423fe4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6bc3ed51-ce10-5e92-b17e-1c3345f16787"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6d0a0938-76e9-5975-9627-6b42a9ac1969"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6d8a15b8-e379-5f10-bfe4-c6e6f1965ab2"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6d9e9d52-f32e-5bfd-bc3d-908955430c86"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6dd1f449-6fa2-5255-a337-ba2f3ace04ee"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6e9ade05-7917-557f-9267-fc99bd7f0c9c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("6ffe65c1-f95a-53ff-947e-048a60f1e50c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("70b6c629-6a6f-54e9-9f54-61db25007bdc"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("70fde0d4-0327-5816-b101-17e689495931"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("714d0e20-3360-542f-b941-ecf7b9ee9f0f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("72111424-f132-5aca-918c-8f19aeb7dc01"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("727a69f5-b978-5668-bcdf-2913476650cf"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("73a2ee6f-100d-50c3-8222-9c10c77ffa8a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("74736870-23c5-5eb3-b460-c29bcec9b5a6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("74df2292-d88f-59b0-be1e-8dc2102d1236"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("7760d494-7063-53b6-b993-f0b26130481f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("78426eac-3c61-536d-9b27-6f2fbee65d96"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("7a56614e-0e7e-5bd7-86db-a340b0b7218a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("7a607b40-1aac-5a21-8d31-e681c8bb3518"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("7a7e50d5-ba7e-5041-b706-70ba74b5b4b3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("7fc90b5a-f5c4-550d-8c12-01b879e4beba"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("82213ff8-477b-54a3-a971-c40eddfe0450"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8275648c-9fd3-50de-91d0-8342ae085419"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("877f3744-5cf6-5e32-950f-51de2581f81a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("88c8c91e-4c13-5747-a733-5e7bd631d2e2"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8939a693-2bf6-586f-aa3a-ca7e2305be3d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("89a1ab45-75d3-5526-a126-b5a7876d5a45"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("89c8f096-3be2-58ca-b68a-a1504e479549"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8ab9f556-0a0f-50a9-bafe-2ab9cc829be3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8b78ef24-8d64-550a-8a6e-84e0ea933b00"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8bcdaace-a625-59e4-a61b-d51d470ccda8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8c0c5e47-5489-58c9-a3d0-73c1cc6255e1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8c842bcf-135f-543d-8f9f-22362180490c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("8dff5b61-675b-55cb-90fd-71cfedca5d42"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("900d5fc3-f23c-51a3-83c3-20713de7c24d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("9089d68f-7155-5197-b221-ef15e6a8a8e4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("937a3222-8858-5bc9-bf4b-ff840f8e6552"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("94347404-c7cb-58e5-8ea7-9348c9a3afd1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("95bf00fe-32ed-55f6-ad93-ca788c0fb9a6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("9760cd27-2777-5c01-806b-949dc907971b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("97d83619-bfdf-54aa-9f07-34e818fe440b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("97eb110a-6720-5b5c-94c1-6c7f72dfe543"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("98f65241-8404-59f8-9305-756e2778a95b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("9a167d4e-24f6-5ec9-a90e-c426f414cf08"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("9c679348-37d3-5687-af04-72678096c966"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("9fdefe60-a1fa-5d9d-9c95-120e2f49503c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a022782b-3594-5608-9634-43184cacea50"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a1b7c3f5-46d1-503a-ae4f-ebe5a76e1dc6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a46beeae-7e7d-5076-b310-580c938430b6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a4a1bda1-d6ef-5819-be2d-3360747285f8"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a4c0490a-f872-558d-a0db-12eec1806460"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a5207ca4-01ba-5506-8bfe-58557c6d58a6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a56bbab0-adb1-5c6b-9548-1c9fb766567e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a8949b34-0ee7-5afa-94f4-e9e7da5a7ac4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a90b8fa7-1f22-5352-b331-da9469603242"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("a932f5f6-bb9a-52c3-b532-c657b8470e96"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("abf7e5cb-0b1c-50f2-94fa-d6415daad51a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ac35087c-9a86-5be9-89df-79af9edb1b0a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("acb31c57-be91-5d11-a68a-d75358cf7d9d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("acf174dd-3bdd-5748-a3b3-ddec80da9992"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ae844dac-df57-524d-93c1-5033909b9a61"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("af7846bf-e5d1-5708-9bf2-53c28d9d40c4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b00cd8dd-bf1d-5e2c-818a-ffbb2f7cca52"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b106dc3e-e248-5ab5-8c70-9dd757244d78"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b13ff228-88db-59c1-b08a-c9db9f593af3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b230d8de-befd-54b7-882a-03a6e7f91590"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b35abee8-0d06-5fbc-80b0-b30c4afc0111"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b3850ad2-7cbf-500a-8402-1d11a2188580"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b437a401-6270-5525-b256-90d3ced0792e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b55dd564-d323-53c7-bb82-09a0871e7418"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b5637593-6f15-5a45-809a-3adaa8d9eb3b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b5a943bb-72c5-555a-8490-3ace00592a56"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b67800a9-15e2-5c96-9eec-12e9587d356b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b7226c93-0337-5104-967f-a4259d74cd94"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b7849a08-a31b-5717-b262-756d66337ff6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b7b96c25-e952-5326-a0ba-1d70cb18f3a0"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b7c6049a-3ab9-574c-aefa-22da889b5191"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("b955f94a-1809-5ad0-a82b-8f1080b021ab"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ba07e277-0da8-5bf6-9023-8bfed1f059c3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("bc13d566-ae47-53ec-ace1-fc53a17c803b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("bcef089d-2145-5fa5-ba99-a1c1c8d69775"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("be3f5d5e-4cbd-54b7-ad0f-cb2627f00bfa"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("bfe3ed7e-fb20-5c07-98bd-549993105f4a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c18dd1fd-bf63-54d4-bdd9-4ec1b33dcf7d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c2580a23-f796-58b2-ba4a-a004e9dac29b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c29c3e7d-e723-5e00-806c-f05c02083ac7"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c2d33bea-ffdf-50f6-b20b-bdb8e482bc71"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c8d781c5-4893-5d08-9645-498d709c4d0c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("c9a1eb90-fb86-527e-974e-22cf238ec5bb"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("cc24dee6-cbe5-58a6-a921-b0a793f48344"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ccf98ec0-50c4-5452-9913-4b11d32aac1c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ce6877bb-cba2-5966-9cea-26ef8f6b27c1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("cf4b0ec9-4559-543b-82f8-de38dc0fd41d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d00c6fa6-049c-502a-804c-4e25a9e45fac"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d08240f6-ba2c-5ad8-a716-505c87bffdca"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d0dace92-bfc7-56c9-9238-371157a51409"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d211e6d8-e1ae-54cc-881b-24d499b108ec"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d2752cfe-6c6b-5f93-b704-e43badeac850"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d277d5f3-3119-5de2-b057-0c24a9a923d6"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d39363f6-6134-548b-bc66-2d0e1a1e0f2a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d3e3ac72-c8f3-5aa8-a854-1f0be4405469"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d6f666f3-ba1d-5e5a-b3a2-9c65291d45a3"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d888beef-abf2-534a-b8ff-4a5575883334"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("d8f7aaa1-ddf9-5ac2-b07f-c9b8b8454f87"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("da18f8ae-8282-5c53-944b-a7569493d60a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("dc202fef-6d9f-57b9-b979-c7a5e258c552"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("dc538040-4f87-5fa6-805a-749a07bb1c31"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("dc87c9e7-e608-5c94-ab7e-ebceb3487f73"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("dffefe45-4628-5aa1-944e-f514f4d43e2f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e16520c2-b078-50b4-8e4a-14adc0ad4f09"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e1a462be-7f03-5032-8c45-14a330a27e65"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e23bd40f-b6b1-5d73-a4ac-24b9db82f71e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e2c896e5-703b-5d02-a1b9-8b5b627e3fe1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e333da14-ce9a-50c2-a189-be5fac17280f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e3610d1e-93a5-5ece-8fd1-1f9815c36047"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e4744294-48a0-5f0b-ab73-aa7998911bfa"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e53c4c04-c3ae-5dc5-a81a-314046bca655"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e56b37ec-5248-5e33-8a8f-7df8b9da99e1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e620f5bb-a75a-5060-916a-51a9bb418115"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e6cd397f-57db-568a-ad50-85db428e3383"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e74ed8bb-74eb-5d66-8f42-87bbd371d17a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e7e1a574-5ede-5560-b798-f07e9a0dd229"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e8101a0a-c031-5be4-9d92-2071f12b149e"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e892b7e1-37f2-5e3b-a03a-9f1b468dea1f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("e9f10d1d-062a-533f-af15-369bde010fe1"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("eaefae52-3f16-56e2-a045-f292170a59cd"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ebc2e3c0-1d34-5e60-8563-35618c9bc9c5"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ed6bb1ec-1360-5c1e-8f27-68bdbaf815b4"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ed970a59-d03a-5e03-aed6-d204770b9102"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ef831d78-fdba-5928-ab42-866dd286a5a7"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f1985998-5463-5a70-aee4-36ed2aea1821"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f1a3b9bd-f023-5279-b92d-9533a53a338f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f253c8b0-e0c9-5745-bf72-65c807dfb04a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f2c2a045-6283-50f6-a53f-5a1e4abb439f"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f417ce96-028d-51f8-8e52-b607ee2f3d5b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f793ae57-5fce-5b0d-be48-674c55d19e33"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f810885c-6da7-5120-94d3-cd7e3d6cc548"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("f9c0face-ba78-5c57-8d32-46de0bf807db"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("fad55d0c-4e18-5a47-b838-fc556ce93915"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("fb24dac1-d4ca-5463-9a03-78704d821529"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("fd28b13c-b141-5b89-8247-afabb3a731cd"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("feb9e0ee-25cc-501a-8afa-72279a60298c"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ff4117a5-5d26-5774-ba41-7013f426389a"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ff55e663-bc03-5dbc-aad5-32cffd012f5d"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ff56a4be-78a7-5f9a-b50f-357dd086751b"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ffbd3c44-1e5d-51f9-a4e4-c7bc3a1ce3fb"));

            migrationBuilder.DeleteData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("ffd6832b-2d22-51c3-90ad-e2f7a5bf6cbe"));

            migrationBuilder.DropColumn(
                name: "region_id",
                table: "partner_areas");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "cities");

            migrationBuilder.DropColumn(
                name: "region_id",
                table: "cities");

            migrationBuilder.AlterColumn<Guid>(
                name: "city_id",
                table: "partner_areas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000202"),
                column: "sort_order",
                value: 2);

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000203"),
                column: "sort_order",
                value: 3);

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000204"),
                column: "sort_order",
                value: 4);

            migrationBuilder.UpdateData(
                table: "cities",
                keyColumn: "id",
                keyValue: new Guid("019a0000-0000-7000-8000-000000000205"),
                column: "sort_order",
                value: 5);

            migrationBuilder.CreateIndex(
                name: "ix_partner_areas_partner_profile_id_city_id_district_id",
                table: "partner_areas",
                columns: new[] { "partner_profile_id", "city_id", "district_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
