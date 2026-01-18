using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMetadataToExtractionCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailFrom",
                table: "extraction_candidates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailMessageIdExternal",
                table: "extraction_candidates",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailReceivedAt",
                table: "extraction_candidates",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EmailSubject",
                table: "extraction_candidates",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailFrom",
                table: "extraction_candidates");

            migrationBuilder.DropColumn(
                name: "EmailMessageIdExternal",
                table: "extraction_candidates");

            migrationBuilder.DropColumn(
                name: "EmailReceivedAt",
                table: "extraction_candidates");

            migrationBuilder.DropColumn(
                name: "EmailSubject",
                table: "extraction_candidates");
        }
    }
}
