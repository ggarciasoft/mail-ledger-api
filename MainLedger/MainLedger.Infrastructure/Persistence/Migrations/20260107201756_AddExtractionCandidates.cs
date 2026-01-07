using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "extraction_candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    extraction_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    merchant = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_account = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    target_account = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_bank = table.Column<string>(type: "jsonb", nullable: true),
                    target_bank = table.Column<string>(type: "jsonb", nullable: true),
                    fees_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    fees_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    tax_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    reference_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    amount_confidence = table.Column<double>(type: "double precision", nullable: true),
                    date_confidence = table.Column<double>(type: "double precision", nullable: true),
                    merchant_confidence = table.Column<double>(type: "double precision", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_extraction_candidates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_extraction_candidates_created_at",
                table: "extraction_candidates",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_extraction_candidates_email_message_id",
                table: "extraction_candidates",
                column: "email_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_extraction_candidates_status",
                table: "extraction_candidates",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "extraction_candidates");
        }
    }
}
