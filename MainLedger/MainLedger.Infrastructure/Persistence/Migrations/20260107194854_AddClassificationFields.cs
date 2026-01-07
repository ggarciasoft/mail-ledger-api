using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "email_messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "classification_confidence",
                table: "email_messages",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "classified_at",
                table: "email_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_financial",
                table: "email_messages",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_messages_category",
                table: "email_messages",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_email_messages_is_financial",
                table: "email_messages",
                column: "is_financial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_messages_category",
                table: "email_messages");

            migrationBuilder.DropIndex(
                name: "ix_email_messages_is_financial",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "category",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "classified_at",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "is_financial",
                table: "email_messages");
        }
    }
}
