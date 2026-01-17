using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSyncHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSyncHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    SyncStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SyncCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailsFound = table.Column<int>(type: "integer", nullable: false),
                    EmailsProcessed = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ProviderMetadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSyncHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailSyncHistories_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailSyncHistories_UserId",
                table: "EmailSyncHistories",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailSyncHistories");
        }
    }
}
