using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemPriceLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_price_locked",
                table: "work_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("005b7ea3-01d7-5465-b951-1c10939fd0fe"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("00fd549e-d4a6-53d4-8578-43f13a9abc7d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0339b5bf-9adb-5065-82c8-6b0dbedd60ba"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0612bbb1-213c-5305-b3ea-2b1322bace9d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("06f5870c-d6ba-5b2b-b8a9-16e31563c57a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("07cc3e8d-6416-5128-86cf-13307e6b3044"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("080808a0-8570-5be3-9974-57261f4d1a21"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("08137ff8-9941-55ba-8a8b-128ce8741213"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0b6f1ab4-07bb-5314-ae38-1477f56b0dcf"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0d121576-10c4-5f17-8014-b94389cb556a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0d4625a9-72a9-51ad-806b-79b63b8478c8"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0f04b279-2a8f-561a-a8c8-897c916fda8e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0f812352-8502-5a80-9b8a-942e22b5ff40"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("0f9d891c-937d-5106-bbeb-1d449d36e70d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("101154b0-ee46-532a-a35b-b2f61d48f2e5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("11f94366-d67c-5b96-9557-0fd3e2f2fe16"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("136eae64-360b-5213-a91d-b906a4ec785f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("16427d1b-6a25-526d-93ad-66572c8d957e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1711a2dd-9fcb-5db5-b3d5-8d753dc9748c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("17d5b708-f5e6-58bf-ac2e-31bcdafb4ac2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("17e79f95-a374-5f5b-8f92-504bad7e28cc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("190f0976-6591-56ed-b048-6d56bd6c4ec2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1a5aa403-6500-589e-b4b3-e5fab159eca1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1abdc3ce-b058-5a1e-aee0-2fd342d95568"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1c41857b-8d24-5279-9d09-8a600ab81998"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1d04bcd5-fcfb-5eb3-9dfa-b1fabf8f5de2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1d40dce5-d948-5a8a-9559-2ce51a4b5035"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1d8b0bc5-7e17-52e7-a05b-2612d45ac434"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1dcaa096-f354-5a11-83e9-11947fb82190"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1e16be33-9310-565e-ac9a-39e5b61f2bcd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("1ff41a10-624b-55cb-b4f5-79d4cd32c1e7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("207cb7c7-d957-5a76-9276-a3e3f3d7b5a5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("20c4f17f-991e-5fc3-b517-12d1691f43f1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("211c90e4-2429-539d-aaa2-3957233bc677"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("22a2a903-ac64-513f-8045-f04a79533ab5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("22dab37f-497e-5341-b6dd-051f5ce18459"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("22edda47-2662-5043-b582-24fe6e455e8b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("235036de-7be6-562b-8712-f95cc397cbeb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("24717f94-392e-5821-8b79-258e27136efc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("248d1c1c-771e-5c89-a39b-197b88066642"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("25cc4a6c-ac3c-5a63-b426-57079059bae3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("25e565be-3390-5511-8000-09c49e5a5a0c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("26aec2c1-435f-5cd4-a6ec-e90bce8de3c0"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("2718331d-d1f0-5e9d-8cb6-560578789329"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("27489883-7c20-5f8d-ab6a-d31f9dd33bbf"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("27b55b4d-73f0-5d29-a786-fa607854bbad"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("28e82c72-b049-5964-b981-2cdb4523bba5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("29fba920-19e5-51fe-a88b-8e4718b058ea"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("2bc1759e-5b47-5830-ba17-7e0554280a63"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("2e39a77f-c4cd-562d-8960-31fe21cc9cfb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("2f438e2a-cf7d-5f6d-9e8d-6f2c94494778"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("2f925487-c521-527f-a0f7-f543f3467baa"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3157ff2e-9ac9-5aba-9c78-dcdef97692bf"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("328694fe-d1f5-57ff-a668-0a92e9612ff1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3417820f-a5fd-55b9-bbec-11f56cad5e53"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("34593697-5dd9-5308-98f0-d5310cc11626"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("352cd5f1-1500-5c48-a500-3545fccc4753"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("368c3c4b-155d-5e28-bc50-39cb2fa346dc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("37660565-e353-54dd-84b4-1b17662ca043"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("37de938d-9a9e-5628-acbb-04ff5bdeb051"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("37ee7081-3836-5679-80b6-3ada931a2ce8"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("38df48cf-2034-5950-ba25-0aa45431b088"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("39203f06-2d8e-5f8c-90ec-b2971bc44b3f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3a3a8d87-1e83-5916-865a-997913144ca6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3a79e4b4-32f4-522d-9926-c7ba3b80ebb0"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3c9414df-65bb-5166-b154-27e015ae3bf1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3eb7b553-1553-5def-a143-8ec7724184e1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3ec5db97-56aa-51d8-8173-29bb6200ecb1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("3f76eb09-9219-53fc-ad97-db7cc0f10658"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("405f53e0-c392-51c2-9a57-34d010d70f3f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("40a4ea57-23cb-5d97-8be1-7c71dd82968e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("40f332de-01ef-596a-a64e-50f1d28d226b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("41e2d2e5-4e19-56be-adb6-d581c858ec82"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4259781c-fea0-550c-8575-16dc5bdb40b1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("43078562-03ff-5f41-833d-1e20669a09f8"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("445006f6-246e-51b7-a92f-f20abd4dbc6b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4573c505-081c-59e4-937e-075e9734ea21"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("45c54bec-1bdd-5fd6-a8aa-446905eac9df"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("460e2f6b-543f-5879-9a17-fbf03f9f24af"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4610ab30-94aa-5fc9-89ab-8cec6b309d85"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("46a94de4-80d9-508e-b2f9-a103c7214639"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("48a1b9f8-fe4d-5690-876e-9c18b2c74187"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("49c03afc-c04c-5fa8-9e9c-ca0085ebf675"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4b2aca87-7685-5fad-bd5f-aaef153df685"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4b4e8317-3c82-5532-8a56-303bdff8d528"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4bbb6d04-f423-5515-961e-24df9ed0c671"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4caea2b9-22d0-5635-923a-62798cc033f5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4ccfd4e4-8df7-586c-9052-083192594994"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4eaae419-375c-5c01-a1a7-67e382e7b16c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("4eb3cc9d-9ac5-5d4c-86a9-21cb094e5f25"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5055d29f-feda-5c61-8ade-4c9c97b14b6b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("538bde2c-6f8c-50be-bf5e-75378fc250f5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("53fe59f3-8feb-5199-ba01-7684e86f1b56"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("54a76255-7f50-5f4d-b433-afe5dc47ec5c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("55408b7a-abf8-57d2-a11b-5dbf916473b8"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5576975f-56a9-5364-8649-f838331c3c31"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("55b7e113-f820-5d4c-9464-4b4ca760ad92"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("57f2dfdd-4cc7-5df1-b20e-0e7b000e3456"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("596e6125-3a31-5272-9260-752f488cc20c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5a226555-88b8-5ec0-b86d-da4d1a95942b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5a24f466-11c3-56a1-b118-fac15dc5b998"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5aff442b-49f6-57c8-8cf3-5fb097e3ff70"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5b5705c1-05ff-503d-97ea-293605528227"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5c9a931f-2b3a-53dd-bb09-c1a0eab980ed"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5f6817d3-c7e6-5979-98d1-0b70438a3d43"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("5fa4e6bb-83e7-5832-b2b7-0d7135d445aa"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6164e6de-7b6f-5e2f-be17-5e09c44d779e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("61f050a0-62cf-533e-a3be-5086e4445519"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("64adf4ac-a385-5e05-8f43-40985d7780ab"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("684da2f4-d8fd-5acd-8db5-5d6d17fa87c7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6abfec99-f06b-5519-9984-93b26d1e8809"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6ad4b5e1-73cb-5085-8b0d-2309d06fe1d9"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6b50be19-4ef5-5f55-87c3-f532aeca9edf"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6bcee1f9-f146-5cc8-ae3c-69e7f19b24e1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6df44158-9c11-52dc-8ea0-8512dd444464"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6eab66e8-b49e-5347-b591-e65c89b0bddb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6eb0879f-09b7-5bb5-87c7-0a22439df06e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6edea08a-7662-57a7-b7ce-3eeb85bca4db"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6f3903df-0cae-5cb6-8b41-1a1ba82634af"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("6ff93d9c-f9ac-5203-a278-eae2c302bd84"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("707ccc67-ce04-5936-a218-2a89e0d581f3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("721b0e22-bc64-54e2-af5a-259452434d84"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("723cd598-b18f-58ab-b4ce-f947d4aed2b9"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("727488ad-a41e-54ad-8e5d-db5d3611fe57"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("72eb416b-769d-5b34-976c-9a7bacaca38b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("73d9b200-d878-53cb-bed4-ee8daba1eed5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("745a0d35-14ab-554a-b853-a0662d95bc40"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("78011376-8c39-5738-b4ff-f66dcec6f9f9"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("78151757-e548-5a57-8172-5278825f2b65"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7849d0a6-0549-5224-9b3a-db456dd24349"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7964f874-2a67-53ca-bdcd-1a24613d744b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("796dfebd-23c0-56e3-8465-683007773893"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("79eda1f6-f154-5574-95db-90ea6eda28d5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7bae6077-691f-5b81-ad96-12a06e45347a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7bc67481-cc45-5bf6-b63f-c1ae773f3605"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7e3d6c11-3fb0-5d80-89ca-588b7f9c86d3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("7f16127a-e2a6-525c-9987-acca0ccf2417"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("811e2038-f7b2-5665-a244-b14bfb659009"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("814767ff-d202-532e-a13f-e2e0d6d6d888"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("82d0c6bb-7db0-53a2-902c-55143eff4fa7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("83d79b3d-0b23-501f-8068-98fd995dbfe2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("83f122b5-0f61-5472-aafc-1cab03f2d4cd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("84e8e174-41c4-59a7-a0b4-3e4cd4dd65a5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("872591c5-9416-5f67-8adf-3dad920850fc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("87736dc2-df27-5bcf-a634-d6baf3d87105"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("87772963-047d-5804-b786-2d4729276926"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8a4fb2a6-3c88-5eec-bcb9-a459acc59201"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8b227c04-0359-58f0-b57f-985c6317e7bb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8bc3ef52-e58e-56ab-9aca-85b00c9dff29"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8bf47f28-ef4f-540d-a711-90a391b0f732"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8c87ba96-1ef8-5e89-a9ad-067c8719be3d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8d3c63ff-f3bc-58b6-94ef-383631a335b2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8d8b2c95-b388-5f84-8cc9-3bc26466a74f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8dbdb9e6-837a-51fa-9305-6ee38f83b226"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8fabb79a-adc9-5099-9b5c-d1a8bba8edeb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8fad2959-9bed-503a-929e-d04f0dfa97f2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("8fc11747-990b-5479-8170-cfb4e0ae33b6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("906cae97-6a91-57fe-925d-74b95b5d1fa6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("91807c55-1f76-5192-807a-f4e4383147bb"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("923db82e-7f63-5977-8d7e-ee9008b450c9"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9280b2ff-5817-52a9-829e-7f47794b34d7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("92d75203-88b3-5296-a761-95ff99b57e34"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("93665c3c-046c-53bc-aaf8-6b08b4168da4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("94c3315b-80b8-5def-9f5d-10120cefdbe0"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("959c2a65-0a58-5bb9-ad27-b23a243e0831"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("973929f0-4259-546e-b65a-9e1fbbcb2adc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9765e006-4645-5893-8f2b-6f6602a0f094"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("98af8d32-fdef-5da2-a67a-c2eee6d03a1e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("99f2bf00-dd94-5cb5-999f-903d32c2be9b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9a084f55-0b41-59b4-b1b2-1b0d8d809013"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9a130167-784a-5f4c-bc0f-b5deffa97ce1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9a539425-85a3-50fc-b611-79f19ff8c732"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9b377e6f-ab80-54a7-ab84-6c689fb93c19"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9c8b1566-a45e-53d7-9a17-e818753d477e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9cfe935d-d9f9-55d8-bc20-647b952ace74"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9d3e65a0-4ae7-5f82-b027-8682905cff66"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9db61447-c6d5-5f7a-8646-02c7d2e546ad"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9e06a0b5-3fe8-5475-8301-6061cdc92484"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9e9c873c-4a19-503a-a41b-5127aea6be00"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9ec73676-167a-5443-ade5-627a949839a6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("9eccfe56-0a6e-554d-8a41-aba698833471"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a30d136f-e01a-567f-af4a-3bfedaa2f4fd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a3855644-7c03-5422-846a-188285d83360"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a3bbb17a-da4d-514f-9321-b047bf709c32"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a53f4d7a-8f86-5d51-b468-ee38abdcc543"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a5d0cad7-41d1-5fbc-b408-2539808eadf7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a67f356b-067a-53f5-b025-ec52dc6e436b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a69cba31-f2ee-5fa0-ac79-653bea95851a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a728f6f6-7267-5bef-90cc-2722bc6bb20e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("a8c194ac-5826-5389-9a87-050c6fd5cb8a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("aa1e03d5-636b-5fa4-af3f-cf53426183f1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("abe5e4ba-6cb8-57a6-9911-048b98b0d4a1"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ac5ed7bc-84e1-51bb-be50-a6016f82f672"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ac954ec4-01b7-54ab-b4c8-46e7d6dac4e4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ad56fcdb-622b-5b33-99d2-6271f077a337"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("addac716-d65d-531c-94b6-9cb7731a4c05"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ae301c42-d86f-5017-be47-5202a1a1784c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ae40705a-a136-51ae-8629-224bba7ab86d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("afb50168-1605-510a-80a7-035056eddd45"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b0e2a76c-4e50-5d0d-b14b-b3e1487c75e7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b15a7d44-fbd7-57d7-b92d-34671ead796a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b45ba603-3098-5b62-bab2-6723327adfa7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b4bb7ab6-6d52-571e-9a57-f5635961d360"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b5d4bff0-cf90-5caf-b486-bed8db8ca0ce"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b6ee2a3f-4f15-5f62-9bf5-5154e21d5461"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("b7c31f92-4659-5a01-b877-ce07914a5328"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ba552c91-9d95-5acb-a9cf-e6c0e623b156"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ba55b36c-9577-5b44-831a-5bcf7848ce9e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("be10c7ad-bf9a-5cfc-a52d-7784414dc88e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("be51713a-de35-5ecf-b7a1-ded73872b4cf"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("bf21ebcf-4e64-5883-b7e5-17778986159a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("bf6245c1-bd8d-5caf-9182-1ffe3205b5b6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c0b8b7ae-7e4c-53cf-bd6b-4441c6d75b95"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c14bea47-3356-5cb2-ab90-b4cbe84ff97d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c1601a3a-c55b-57e7-b10d-e7d00b098993"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c17b2078-6ed1-5e73-af9e-972d95465a83"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c48d089e-8ff4-56c0-a0da-cdfc80fcf0ea"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c4d36796-9eef-55b4-a25c-01321465343b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c54b857f-a147-50cc-803e-ca5ae2ba71f7"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c65f69d0-3e04-51de-b41a-563899d92ab6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c66c8540-e22c-5cd6-a7e7-4247c07d4af4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c68e061c-25e3-5455-937f-a7bc9abd4e5d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c7a317af-b3db-5b0d-9317-52e62e394d97"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c7b89a66-75ca-5cb3-b90d-f45f3653e1ba"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c84742b2-7692-5b4a-b41c-5c585334d0af"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c8b55d98-d000-5c4b-aafb-3a806577b4d3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c8eb3f2c-5388-5d26-8abb-5addf15588c4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c92bdb7a-3514-5f5f-982f-b8b5873c08d5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("c9fd15f2-52a2-56f3-9f7b-dffbefbc36da"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ca435290-d3f1-5726-9968-45dc89a39c2a"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("cad2e150-242d-500a-b332-f0ca934225bd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("cbaa0c31-b390-5955-81ec-bb4d7fffc1a3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("cbbeac89-77e7-5bbe-a65e-9570c4a803e0"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("cc173b43-e129-5840-8969-4356f262d830"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d23b912d-9d08-5894-be14-3aef9eebb7b3"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d2aeed5d-66c9-5e1d-a4a1-668f4a17cb88"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d4726908-97b9-5efc-ab52-a0125c757245"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d5252b14-5c85-5b1d-aed0-134a04d1c7aa"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d5721113-01db-5910-b298-e1c817a16172"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d7ae22c4-80a4-5fcb-9d33-affe3abf043d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d970e65a-433a-5617-9a38-dc161599d372"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("d9c7dc35-9cfa-5dd9-94fe-3e37a8a6028e"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("dcad5505-ef9a-5ff3-8406-cfac68bebbb2"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("dcd8fc75-66d0-5122-850c-1320e7fc9dc6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("dce76d5c-4e3b-5150-bc4e-66d52c8dcc37"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ddc04332-baca-5399-b4b8-4a6d002e14f6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("de7bb175-4ece-5767-84bf-bbb921eba2ea"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("df07d90f-2ae3-5b2f-8154-daba46d0bc2d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("df929533-1208-5115-8bab-7b6968bb3896"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e13064fc-8f84-5d62-b3fb-b8697b5d97a4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e15b8774-12b8-55b8-b77e-c68848ca7946"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e1f587a2-528d-5e51-99a5-2c3bc8d030dd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e33b3920-a5d5-5a34-8f64-96e3589a744b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e36c2eee-bfdb-5eb2-8470-3d0e46fbb3bc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e3968fc2-0493-57fe-9f1b-07b57a0224bd"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e43141bd-c1d3-5d81-a623-aa9e95058084"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e454c633-b0f0-581d-90a5-9c5ca882e8c8"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e4e8c4a0-fb3d-596e-83bd-a1f67c3966c6"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e672be9e-3dc0-5aac-a074-5de3106381ae"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e69e6b5b-07a4-576c-a515-bb1bf871382d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("e8d533ab-5e82-57d4-8df6-6a949341c75f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("eb89ce3c-627a-5982-8ff8-ffd143942f72"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("eb9017a9-ae49-5576-a4de-801a0b757ee5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ec9577d1-762f-56d3-9028-9804b3d27d88"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ec9d7f4a-71b2-5ad0-89c4-ba0d615c9512"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("eca1dff9-b194-5160-9651-38245ff00885"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ed4cf38f-9aa6-5f5a-8a87-513d4cbecaa5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ef57ae71-d978-57d8-93bf-3b6a10b4240d"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("efa8ce87-9889-5148-b931-2d731c5de842"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f024f08f-a094-54a0-8c2c-7443755c458c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f105cf7f-81bf-5f92-87c6-16d0e76bb84b"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f1da9962-b83d-5427-84e3-1bd52bdbfabc"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f511787d-6481-5974-afa7-3d7215fd4e79"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f59e6c07-8429-58de-9cd5-6e7458f4c379"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f6f05543-d5a9-5ba2-8b0a-f8186a40caec"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f79559c3-2a18-5b9d-881a-7d880a3bdfa4"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f8d31f97-6830-5eb1-987e-6ef5d8ea0520"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f984bd06-2505-553e-8667-fa7156f1f730"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("f9852146-0725-52c0-9475-6e162bd52508"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fa20ad78-0bd5-5366-869e-ce68225f92e5"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fade1600-d8b0-538d-9063-0ae9db772b8c"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fc5c9b5d-8c58-5741-89b6-e705563e5f7f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fc6ba612-c8c2-5664-ae84-c7932935f067"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fd9489a6-259a-5749-8771-9b8bdaa0241f"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fe4efe52-a0b7-525d-826e-5df750383d19"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("fe78cd39-a7e3-5da5-aee6-5cff4b397e33"),
                column: "is_price_locked",
                value: false);

            migrationBuilder.UpdateData(
                table: "work_items",
                keyColumn: "id",
                keyValue: new Guid("ff2a17f2-3198-52ae-a14b-4fa59b27e7af"),
                column: "is_price_locked",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_price_locked",
                table: "work_items");
        }
    }
}
