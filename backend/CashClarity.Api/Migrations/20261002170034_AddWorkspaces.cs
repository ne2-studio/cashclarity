using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashClarity.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "accounts_user_id_code_key",
                table: "accounts");

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspaces", x => x.id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "bank_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "workspace_id",
                table: "accounts",
                type: "uuid",
                nullable: true);

            // Backfill: every user with existing data gets one default workspace ("Principal")
            // that adopts all of their accounts, journal entries and bank movements.
            migrationBuilder.Sql("""
                INSERT INTO workspaces (id, name, user_id)
                SELECT gen_random_uuid(), 'Principal', u.user_id
                FROM (
                    SELECT user_id FROM accounts
                    UNION SELECT user_id FROM journal_entries
                    UNION SELECT user_id FROM bank_movements
                ) u
                """);
            migrationBuilder.Sql("""
                UPDATE journal_entries t SET workspace_id = w.id
                FROM workspaces w WHERE w.user_id = t.user_id
                """);
            migrationBuilder.Sql("""
                UPDATE bank_movements t SET workspace_id = w.id
                FROM workspaces w WHERE w.user_id = t.user_id
                """);
            migrationBuilder.Sql("""
                UPDATE accounts t SET workspace_id = w.id
                FROM workspaces w WHERE w.user_id = t.user_id
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id",
                table: "journal_entries",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id",
                table: "bank_movements",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "workspace_id",
                table: "accounts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_journal_entries_workspace_id",
                table: "journal_entries",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "idx_bank_movements_workspace_id",
                table: "bank_movements",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "accounts_workspace_id_code_key",
                table: "accounts",
                columns: new[] { "workspace_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_workspaces_user_id",
                table: "workspaces",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_accounts_workspaces_workspace_id",
                table: "accounts",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_bank_movements_workspaces_workspace_id",
                table: "bank_movements",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_journal_entries_workspaces_workspace_id",
                table: "journal_entries",
                column: "workspace_id",
                principalTable: "workspaces",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accounts_workspaces_workspace_id",
                table: "accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_bank_movements_workspaces_workspace_id",
                table: "bank_movements");

            migrationBuilder.DropForeignKey(
                name: "FK_journal_entries_workspaces_workspace_id",
                table: "journal_entries");

            migrationBuilder.DropTable(
                name: "workspaces");

            migrationBuilder.DropIndex(
                name: "idx_journal_entries_workspace_id",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "idx_bank_movements_workspace_id",
                table: "bank_movements");

            migrationBuilder.DropIndex(
                name: "accounts_workspace_id_code_key",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "bank_movements");

            migrationBuilder.DropColumn(
                name: "workspace_id",
                table: "accounts");

            migrationBuilder.CreateIndex(
                name: "accounts_user_id_code_key",
                table: "accounts",
                columns: new[] { "user_id", "code" },
                unique: true);
        }
    }
}
