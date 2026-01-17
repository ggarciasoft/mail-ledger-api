using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailSyncHistoryAndRemoveGmailSyncHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GmailSyncHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GmailSyncHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailsFound = table.Column<int>(type: "integer", nullable: false),
                    EmailsProcessed = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    LastHistoryId = table.Column<string>(type: "text", nullable: true),
                    SyncCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GmailSyncHistories", x => x.Id);
                });
        }
    }
}
