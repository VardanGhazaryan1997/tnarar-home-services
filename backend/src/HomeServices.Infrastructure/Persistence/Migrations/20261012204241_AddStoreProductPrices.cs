using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProductPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_product_prices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    sale_price = table.Column<int>(type: "integer", nullable: true),
                    sale_ends_on = table.Column<DateOnly>(type: "date", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_product_prices", x => x.id);
                    table.CheckConstraint("ck_store_product_prices_price", "price >= 1");
                    table.ForeignKey(
                        name: "fk_store_product_prices_store_products_store_product_id",
                        column: x => x.store_product_id,
                        principalTable: "store_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_store_product_prices_store_product_id_recorded_at",
                table: "store_product_prices",
                columns: new[] { "store_product_id", "recorded_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "store_product_prices");
        }
    }
}
