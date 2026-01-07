using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailProcessingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "processing_error",
                table: "email_messages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "processing_status",
                table: "email_messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_email_messages_processing_status",
                table: "email_messages",
                column: "processing_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_messages_processing_status",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "processing_error",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "processing_status",
                table: "email_messages");
        }
    }
}
