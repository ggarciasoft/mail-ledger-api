using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameMaxGmailAccountsToMaxEmailAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "max_gmail_accounts",
                table: "subscription_plans",
                newName: "max_email_accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "max_email_accounts",
                table: "subscription_plans",
                newName: "max_gmail_accounts");
        }
    }
}
