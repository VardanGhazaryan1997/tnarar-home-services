using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UiTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "ak_languages_code",
                table: "languages",
                column: "code");

            migrationBuilder.CreateTable(
                name: "ui_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(32)", maxLength: 32, nullable: false),
                    key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    language_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ui_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ui_translations_language_code",
                table: "ui_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "ix_ui_translations_namespace_key_language_code",
                table: "ui_translations",
                columns: new[] { "namespace", "key", "language_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ui_translations_namespace_language_code",
                table: "ui_translations",
                columns: new[] { "namespace", "language_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ui_translations");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_languages_code",
                table: "languages");
        }
    }
}
