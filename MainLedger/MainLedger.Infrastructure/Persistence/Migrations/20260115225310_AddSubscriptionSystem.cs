using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "api_keys",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "subscription_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    monthly_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    monthly_email_limit = table.Column<int>(type: "integer", nullable: false),
                    max_gmail_accounts = table.Column<int>(type: "integer", nullable: false),
                    max_api_keys = table.Column<int>(type: "integer", nullable: false),
                    history_retention_days = table.Column<int>(type: "integer", nullable: false),
                    can_export = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_workflow_automation = table.Column<bool>(type: "boolean", nullable: false),
                    can_use_webhooks = table.Column<bool>(type: "boolean", nullable: false),
                    max_webhooks = table.Column<int>(type: "integer", nullable: false),
                    can_use_bulk_operations = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    emails_processed_this_month = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_subscription_plans_subscription_plan_id",
                        column: x => x.subscription_plan_id,
                        principalTable: "subscription_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_is_active",
                table: "subscription_plans",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_plans_name",
                table: "subscription_plans",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_status",
                table: "user_subscriptions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_status_end_date",
                table: "user_subscriptions",
                columns: new[] { "status", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_subscription_plan_id",
                table: "user_subscriptions",
                column: "subscription_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_user_id",
                table: "user_subscriptions",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "subscription_plans");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "api_keys");
        }
    }
}
