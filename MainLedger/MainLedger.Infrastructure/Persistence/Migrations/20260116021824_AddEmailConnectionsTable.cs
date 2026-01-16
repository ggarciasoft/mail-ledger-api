using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConnectionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmailConnectionId",
                table: "email_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    encrypted_access_token = table.Column<string>(type: "text", nullable: false),
                    encrypted_refresh_token = table.Column<string>(type: "text", nullable: false),
                    token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_connections", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_connections_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_messages_EmailConnectionId",
                table: "email_messages",
                column: "EmailConnectionId");

            migrationBuilder.CreateIndex(
                name: "ix_email_connections_is_active",
                table: "email_connections",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_email_connections_user_id",
                table: "email_connections",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_connections_user_provider",
                table: "email_connections",
                columns: new[] { "user_id", "provider" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_email_messages_email_connections_EmailConnectionId",
                table: "email_messages",
                column: "EmailConnectionId",
                principalTable: "email_connections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_messages_email_connections_EmailConnectionId",
                table: "email_messages");

            migrationBuilder.DropTable(
                name: "email_connections");

            migrationBuilder.DropIndex(
                name: "IX_email_messages_EmailConnectionId",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "EmailConnectionId",
                table: "email_messages");
        }
    }
}
