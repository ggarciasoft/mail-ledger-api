using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesEngineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "directive",
                table: "email_messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "directive_reason",
                table: "email_messages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "matched_rule_id",
                table: "email_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_messages_directive",
                table: "email_messages",
                column: "directive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_email_messages_directive",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "directive",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "directive_reason",
                table: "email_messages");

            migrationBuilder.DropColumn(
                name: "matched_rule_id",
                table: "email_messages");
        }
    }
}
