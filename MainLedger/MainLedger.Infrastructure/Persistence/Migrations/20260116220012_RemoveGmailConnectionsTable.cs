using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGmailConnectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gmail_connections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gmail_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    history_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refresh_token_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gmail_connections", x => x.id);
                    table.ForeignKey(
                        name: "FK_gmail_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gmail_connections_is_active",
                table: "gmail_connections",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_gmail_connections_user_id",
                table: "gmail_connections",
                column: "user_id");
        }
    }
}
