using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CashClarity.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBankLinksAndExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "bank_movements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bank_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    workspace_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    aspsp_name = table.Column<string>(type: "text", nullable: false),
                    aspsp_country = table.Column<string>(type: "text", nullable: false),
                    psu_type = table.Column<string>(type: "text", nullable: false),
                    sync_from = table.Column<DateOnly>(type: "date", nullable: false),
                    pending_state = table.Column<string>(type: "text", nullable: true),
                    pending_state_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    session_id = table.Column<string>(type: "text", nullable: true),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    account_options = table.Column<string>(type: "jsonb", nullable: true),
                    account_uid = table.Column<string>(type: "text", nullable: true),
                    iban = table.Column<string>(type: "text", nullable: true),
                    account_name = table.Column<string>(type: "text", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: true),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_bank_links_workspaces_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "bank_movements_workspace_id_external_id_key",
                table: "bank_movements",
                columns: new[] { "workspace_id", "external_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "bank_links_workspace_id_key",
                table: "bank_links",
                column: "workspace_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_links");

            migrationBuilder.DropIndex(
                name: "bank_movements_workspace_id_external_id_key",
                table: "bank_movements");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "bank_movements");
        }
    }
}
