using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCategoriesToGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_user_id",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "idx_categories_user_id_name",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "categories",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "idx_categories_user_id",
                table: "categories",
                newName: "IX_categories_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_UserId",
                table: "categories");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "categories",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_categories_UserId",
                table: "categories",
                newName: "idx_categories_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_categories_user_id_name",
                table: "categories",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_user_id",
                table: "categories",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
