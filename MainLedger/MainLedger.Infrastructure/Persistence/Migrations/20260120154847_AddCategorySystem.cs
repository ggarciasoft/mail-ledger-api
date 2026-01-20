using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "financial_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "extraction_candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_records_CategoryId",
                table: "financial_records",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_extraction_candidates_CategoryId",
                table: "extraction_candidates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "idx_categories_user_id",
                table: "categories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_categories_user_id_name",
                table: "categories",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_extraction_candidates_categories_CategoryId",
                table: "extraction_candidates",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_records_categories_CategoryId",
                table: "financial_records",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_extraction_candidates_categories_CategoryId",
                table: "extraction_candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_financial_records_categories_CategoryId",
                table: "financial_records");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropIndex(
                name: "IX_financial_records_CategoryId",
                table: "financial_records");

            migrationBuilder.DropIndex(
                name: "IX_extraction_candidates_CategoryId",
                table: "extraction_candidates");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "financial_records");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "extraction_candidates");
        }
    }
}
