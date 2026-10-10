using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    work_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: true),
                    quantity_per_square_meter = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_room_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_room_templates_work_items_work_item_id",
                        column: x => x.work_item_id,
                        principalTable: "work_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "room_templates",
                columns: new[] { "id", "quantity", "quantity_per_square_meter", "room_type", "sort_order", "work_item_id" },
                values: new object[,]
                {
                    { new Guid("0223a0e8-da77-c81f-1354-c0530f4db5a9"), 1m, null, "Kitchen", 8, new Guid("39203f06-2d8e-5f8c-90ec-b2971bc44b3f") },
                    { new Guid("02d20c23-febd-8e8c-281f-acb18d84bbd6"), 2m, null, "Garage", 4, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("03edc2f6-e519-c499-0959-73c8ce5c230d"), 1m, null, "Office", 9, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("03f45fa7-628b-5844-fc21-ce163f3d90da"), null, null, "Bedroom", 6, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("045f3b5a-9430-2c5b-97a8-fafa6dfa02e6"), 2m, null, "LivingRoom", 8, new Guid("1c41857b-8d24-5279-9d09-8a600ab81998") },
                    { new Guid("047130c6-8a85-7c41-8e77-5b38a2ce6413"), null, null, "Bedroom", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("0c5390d6-be77-92e5-a123-15024a86c563"), null, null, "Toilet", 2, new Guid("be10c7ad-bf9a-5cfc-a52d-7784414dc88e") },
                    { new Guid("0cd7bd00-dfee-7c54-7f8b-421182b0bcdf"), 1m, null, "LivingRoom", 9, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("0d942a68-6971-b574-756c-80689e1e0a49"), null, null, "LivingRoom", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("100d8bcc-05e3-a283-9249-822eaa08fe8c"), null, null, "Office", 5, new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8") },
                    { new Guid("11afe4a3-6e18-f4c5-397b-91109b2ace59"), null, null, "Bathroom", 2, new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e") },
                    { new Guid("18ab4acf-388a-6dd5-7eee-6a5168031820"), 2m, null, "Office", 8, new Guid("1c41857b-8d24-5279-9d09-8a600ab81998") },
                    { new Guid("19682550-25a0-d42d-1750-6889d77ac766"), 1m, null, "Kitchen", 10, new Guid("f1da9962-b83d-5427-84e3-1bd52bdbfabc") },
                    { new Guid("19835805-0294-9046-a447-937d31e1e4ae"), 1m, null, "Toilet", 8, new Guid("abe5e4ba-6cb8-57a6-9911-048b98b0d4a1") },
                    { new Guid("1ba105ee-8972-ea19-2b9c-3b1122384e22"), null, 0.25m, "KidsRoom", 7, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("1ea22a63-cc9b-9271-0938-2193130d601a"), 1m, null, "Toilet", 6, new Guid("ad56fcdb-622b-5b33-99d2-6271f077a337") },
                    { new Guid("21de8fcd-f54e-6ab9-367a-5ef37d61841a"), 1m, null, "Hallway", 8, new Guid("8dbdb9e6-837a-51fa-9305-6ee38f83b226") },
                    { new Guid("24c4441e-9c36-2537-d26c-d993cc0a4cde"), null, null, "Hallway", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("254be657-839c-2538-c4ff-a08034d37996"), 1m, null, "Bathroom", 9, new Guid("cbbeac89-77e7-5bbe-a65e-9570c4a803e0") },
                    { new Guid("26307b64-c074-161b-7e61-1eb23c32b6d3"), null, null, "Balcony", 3, new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e") },
                    { new Guid("32dcb25b-2367-6905-a985-9a5650f61a56"), 2m, null, "Hallway", 6, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("33005e51-4daa-b751-d777-9ae083ef2a5a"), null, null, "Other", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("3353f8b6-70f8-3386-d3b6-1722bba5c093"), null, 0.25m, "LivingRoom", 7, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("34ee14dc-6075-6abe-7c6b-d00b80e72ea2"), null, null, "LivingRoom", 3, new Guid("c65f69d0-3e04-51de-b41a-563899d92ab6") },
                    { new Guid("37f41c22-589a-2c52-2b52-0d714a9216cb"), null, null, "Office", 4, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("3811e9a4-107c-acbb-5afa-f3ab2164933c"), 3m, null, "Bathroom", 6, new Guid("e3968fc2-0493-57fe-9f1b-07b57a0224bd") },
                    { new Guid("3893fd2d-d98d-0590-41cd-4dc408adebcd"), 2m, null, "Garage", 5, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("3e690e94-77e6-9dcc-74a0-34a85d604718"), 2m, null, "Bathroom", 10, new Guid("abe5e4ba-6cb8-57a6-9911-048b98b0d4a1") },
                    { new Guid("3f27bc88-216d-e703-7c72-d17a00862e0c"), null, null, "Bathroom", 4, new Guid("c54b857f-a147-50cc-803e-ca5ae2ba71f7") },
                    { new Guid("4282f0f4-eb0d-45af-c326-ab9740f6ed1e"), null, null, "Other", 4, new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8") },
                    { new Guid("458845b5-1d96-9492-1bf0-660ff1f7b660"), 1m, null, "Bedroom", 9, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("47db5205-38ec-125c-02fa-2203ed4b662d"), 4m, null, "Bathroom", 5, new Guid("c0b8b7ae-7e4c-53cf-bd6b-4441c6d75b95") },
                    { new Guid("4ff76bc4-7c6b-9ba5-8801-083fc1185c5a"), null, null, "Toilet", 1, new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e") },
                    { new Guid("5054d9a9-1a57-be80-083f-572b59b1253f"), null, null, "LivingRoom", 6, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("5394bdc8-8209-3d57-4cbb-61a6b9d67a2e"), 2m, null, "KidsRoom", 8, new Guid("1c41857b-8d24-5279-9d09-8a600ab81998") },
                    { new Guid("55e59754-e1b2-edc7-1a5e-58cbb14bb242"), null, null, "Balcony", 2, new Guid("40f332de-01ef-596a-a64e-50f1d28d226b") },
                    { new Guid("57f8196f-a848-5441-ab8b-f65a564132bb"), null, null, "KidsRoom", 4, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("5d040def-2bf4-a1ec-4348-dcb631666ec4"), null, 1.5m, "Balcony", 1, new Guid("973929f0-4259-546e-b65a-9e1fbbcb2adc") },
                    { new Guid("5d63263b-883c-995d-4ac5-64715b0083e0"), null, null, "Balcony", 4, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("5e65025f-bc01-d4ea-8813-27dbbf513995"), 1m, null, "Toilet", 7, new Guid("39203f06-2d8e-5f8c-90ec-b2971bc44b3f") },
                    { new Guid("609fa236-63b1-ac67-d9ed-59dd631498d6"), 1m, null, "Bathroom", 8, new Guid("39203f06-2d8e-5f8c-90ec-b2971bc44b3f") },
                    { new Guid("670df6ee-0dbe-2ae7-dfb4-9d1b55ef1cc3"), 2m, null, "Bedroom", 8, new Guid("1c41857b-8d24-5279-9d09-8a600ab81998") },
                    { new Guid("69b01bb8-8c4b-2f1a-615f-0e35ce5b066b"), 3m, null, "Kitchen", 5, new Guid("2bc1759e-5b47-5830-ba17-7e0554280a63") },
                    { new Guid("6ec42676-8fb8-d84c-d07f-9ae2410e2d04"), null, 0.25m, "Office", 7, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("734baa2c-1bd7-4a48-3d91-d6ff3e0680cb"), null, null, "Hallway", 5, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("74d99816-dd49-53e7-213c-9a164dab8fb8"), null, null, "Toilet", 3, new Guid("c54b857f-a147-50cc-803e-ca5ae2ba71f7") },
                    { new Guid("7aa873b7-0e2a-6e37-5ed5-6eaa83967f88"), null, null, "Kitchen", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("7c0332ad-9055-1996-80d7-4ba8f5a17d93"), null, null, "KidsRoom", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("856da13c-1488-89ea-7a2e-a61c258ebd43"), null, null, "Other", 5, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("88d260f9-f3e8-08b6-f82e-fd7a3548f481"), null, null, "Garage", 1, new Guid("45c54bec-1bdd-5fd6-a8aa-446905eac9df") },
                    { new Guid("8a99d154-fa57-0876-2a38-5810b699a136"), null, null, "KidsRoom", 3, new Guid("c65f69d0-3e04-51de-b41a-563899d92ab6") },
                    { new Guid("937ddba9-2521-1a80-b19f-5a0cac054ba5"), 1m, null, "Kitchen", 9, new Guid("abe5e4ba-6cb8-57a6-9911-048b98b0d4a1") },
                    { new Guid("95d0f1df-b835-88b6-550b-52ea4fc24f86"), null, null, "Hallway", 3, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("97b026af-51cc-5cda-d387-1e5e53e3e7ab"), null, null, "LivingRoom", 4, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("9a94c8d4-b163-8ee3-41c1-d54a0e26ae8f"), null, null, "Office", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("a35c271d-032a-9967-7ff2-a4b5430bfe95"), 1m, null, "Bathroom", 7, new Guid("ad56fcdb-622b-5b33-99d2-6271f077a337") },
                    { new Guid("a5084f57-314f-52f2-dc70-af83885dbbd6"), 6m, null, "Kitchen", 6, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("a7c9ebb1-4f60-44f7-d2ef-5efe0cf057e5"), null, null, "Kitchen", 4, new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e") },
                    { new Guid("a8c9ccd4-ceeb-fee9-fce2-9e9e41b5c49b"), 4m, null, "Kitchen", 7, new Guid("00fd549e-d4a6-53d4-8578-43f13a9abc7d") },
                    { new Guid("acb08cb8-6490-e844-1249-f62082b9c26f"), null, null, "Kitchen", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("afe94636-8659-c2e9-7d9c-188b1bb3c0f2"), null, null, "Bedroom", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("b2081cbe-023b-3b8b-1a83-834b9e5a9037"), null, null, "Kitchen", 3, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("b3410476-cbc2-3743-1476-490c9c1e5b59"), null, null, "Garage", 2, new Guid("207cb7c7-d957-5a76-9276-a3e3f3d7b5a5") },
                    { new Guid("b3e91df8-90df-08cb-e9af-59569a5220da"), null, null, "Bathroom", 1, new Guid("ff2a17f2-3198-52ae-a14b-4fa59b27e7af") },
                    { new Guid("b847d9e9-7bc3-a8fa-02b2-0340cab9f398"), null, null, "LivingRoom", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("b88654b4-c8dd-fd5d-697b-4b757b08223a"), 1m, null, "Garage", 6, new Guid("53fe59f3-8feb-5199-ba01-7684e86f1b56") },
                    { new Guid("baa5553f-ee7f-d5b9-36d6-1cf3c50ad934"), null, null, "KidsRoom", 5, new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8") },
                    { new Guid("bc49a640-57f1-7ed3-7e32-88cd96d896ae"), null, null, "Bedroom", 3, new Guid("c65f69d0-3e04-51de-b41a-563899d92ab6") },
                    { new Guid("bc84550a-303f-e514-f8f6-69f69a9f889e"), 2m, null, "Toilet", 5, new Guid("e3968fc2-0493-57fe-9f1b-07b57a0224bd") },
                    { new Guid("beb7dd30-50aa-c458-b099-7456c6329a59"), 1m, null, "KidsRoom", 9, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("bed220ff-1fa9-3255-b9d8-1e27a6059a85"), 1m, null, "Bathroom", 12, new Guid("57f2dfdd-4cc7-5df1-b20e-0e7b000e3456") },
                    { new Guid("bedbe4f2-133b-5663-9621-4fc0973ead39"), null, 0.25m, "Bedroom", 7, new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66") },
                    { new Guid("bf7cdde9-cb1c-461e-5e43-d9941695de40"), null, null, "Bedroom", 4, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("c982c497-7fbd-28b6-e591-0a8fb83a641c"), null, null, "Other", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("cbba3a61-d5e5-0c67-1113-7a8f51b0df05"), 1m, null, "Kitchen", 11, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("cce88f86-2af8-667a-0a85-af4ac2d4503a"), null, null, "Hallway", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("d152c421-88a0-7ca5-3fd7-636bdfe32433"), 1m, null, "Hallway", 7, new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd") },
                    { new Guid("d3fea1e8-cfd3-78cb-22c6-c4a3143fc983"), null, null, "KidsRoom", 6, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("d549257f-8862-8a05-6957-1ce5a446b1f9"), null, null, "Hallway", 4, new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e") },
                    { new Guid("d6d4e5ea-cae2-2eb1-d029-859fb85bf0cc"), null, null, "Bathroom", 3, new Guid("be10c7ad-bf9a-5cfc-a52d-7784414dc88e") },
                    { new Guid("d7c86c11-1b1f-3346-59bb-fb2266a92a82"), null, null, "KidsRoom", 2, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") },
                    { new Guid("d9bec76c-9d34-95b9-a9fe-06133cd178de"), null, null, "Office", 1, new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8") },
                    { new Guid("d9dc3b1d-02a4-04ea-2bad-317e04d7b037"), 1m, null, "Bathroom", 11, new Guid("73d9b200-d878-53cb-bed4-ee8daba1eed5") },
                    { new Guid("e95b7d34-d873-234c-1888-e0835ffcec67"), null, null, "LivingRoom", 5, new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8") },
                    { new Guid("ef47839d-8fc6-72bd-b171-de22888c1327"), null, null, "Other", 3, new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3") },
                    { new Guid("f06a6014-ae10-dbe1-6bec-ae828a087dc1"), null, null, "Office", 6, new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3") },
                    { new Guid("f25cf9ee-780d-f928-937a-11bc0eec5c32"), null, null, "Bedroom", 5, new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8") },
                    { new Guid("f8d3b5ce-554d-5f53-2047-60655bc10d87"), 2m, null, "Toilet", 4, new Guid("c0b8b7ae-7e4c-53cf-bd6b-4441c6d75b95") },
                    { new Guid("f9b05f1e-8821-e42b-d0d1-495c7dedb8d1"), null, null, "Office", 3, new Guid("c65f69d0-3e04-51de-b41a-563899d92ab6") },
                    { new Guid("fd80a1a8-e8f8-f0ba-d775-ecac70cef29f"), null, null, "Garage", 3, new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf") }
                });

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3"),
                column: "surface",
                value: "Floor");

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("be51713a-de35-5ecf-b7a1-ded73872b4cf"),
                column: "surface",
                value: "Floor");

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f105cf7f-81bf-5f92-87c6-16d0e76bb84b"),
                column: "surface",
                value: "Ceiling");

            migrationBuilder.CreateIndex(
                name: "ix_room_templates_room_type_work_item_id",
                table: "room_templates",
                columns: new[] { "room_type", "work_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_room_templates_work_item_id",
                table: "room_templates",
                column: "work_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "room_templates");

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3"),
                column: "surface",
                value: "None");

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("be51713a-de35-5ecf-b7a1-ded73872b4cf"),
                column: "surface",
                value: "None");

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f105cf7f-81bf-5f92-87c6-16d0e76bb84b"),
                column: "surface",
                value: "None");
        }
    }
}
