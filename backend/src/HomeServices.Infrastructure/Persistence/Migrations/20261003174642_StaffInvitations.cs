using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeServices.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaffInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "invite_expires_at",
                table: "staff_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "invite_token_hash",
                table: "staff_users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_staff_users_invite_token_hash",
                table: "staff_users",
                column: "invite_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_staff_users_invite_token_hash",
                table: "staff_users");

            migrationBuilder.DropColumn(
                name: "invite_expires_at",
                table: "staff_users");

            migrationBuilder.DropColumn(
                name: "invite_token_hash",
                table: "staff_users");
        }
    }
}
