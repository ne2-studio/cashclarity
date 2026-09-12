using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashClarity.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestrictAccountDeletionWithJournalLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_lines_accounts_account_id",
                table: "journal_lines");

            migrationBuilder.AddForeignKey(
                name: "FK_journal_lines_accounts_account_id",
                table: "journal_lines",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_lines_accounts_account_id",
                table: "journal_lines");

            migrationBuilder.AddForeignKey(
                name: "FK_journal_lines_accounts_account_id",
                table: "journal_lines",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
