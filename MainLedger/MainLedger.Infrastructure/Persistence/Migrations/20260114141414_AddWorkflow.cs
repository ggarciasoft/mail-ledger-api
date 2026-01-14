using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_configurations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    email_sync_schedule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    classification_schedule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    extraction_schedule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pipeline_schedule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    email_sync_batch_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    classification_batch_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    extraction_batch_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_workflow_configurations_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_configurations_user_id",
                table: "workflow_configurations",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_configurations");
        }
    }
}
