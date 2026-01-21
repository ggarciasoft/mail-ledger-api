using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitEmailLimitsIntoClassificationAndExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "emails_processed_this_month",
                table: "user_subscriptions",
                newName: "emails_extracted_this_month");

            migrationBuilder.RenameColumn(
                name: "monthly_email_limit",
                table: "subscription_plans",
                newName: "extraction_limit");

            migrationBuilder.AddColumn<int>(
                name: "emails_classified_this_month",
                table: "user_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "classification_limit",
                table: "subscription_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emails_classified_this_month",
                table: "user_subscriptions");

            migrationBuilder.DropColumn(
                name: "classification_limit",
                table: "subscription_plans");

            migrationBuilder.RenameColumn(
                name: "emails_extracted_this_month",
                table: "user_subscriptions",
                newName: "emails_processed_this_month");

            migrationBuilder.RenameColumn(
                name: "extraction_limit",
                table: "subscription_plans",
                newName: "monthly_email_limit");
        }
    }
}
