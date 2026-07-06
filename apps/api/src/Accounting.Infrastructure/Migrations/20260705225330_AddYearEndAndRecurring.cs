using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddYearEndAndRecurring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsYearEndClosing",
                table: "journal_entries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "recurring_journal_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    NextDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_journal_entries_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "year_end_closings",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_year_end_closings", x => new { x.OrganizationId, x.Year });
                    table.ForeignKey(
                        name: "FK_year_end_closings_journal_entries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_year_end_closings_users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recurring_journal_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Credit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_journal_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_journal_lines_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_journal_lines_recurring_journal_entries_Recurring~",
                        column: x => x.RecurringEntryId,
                        principalTable: "recurring_journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_journal_entries_OrganizationId",
                table: "recurring_journal_entries",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_journal_lines_AccountId",
                table: "recurring_journal_lines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_journal_lines_RecurringEntryId",
                table: "recurring_journal_lines",
                column: "RecurringEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_year_end_closings_ClosedByUserId",
                table: "year_end_closings",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_year_end_closings_JournalEntryId",
                table: "year_end_closings",
                column: "JournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recurring_journal_lines");

            migrationBuilder.DropTable(
                name: "year_end_closings");

            migrationBuilder.DropTable(
                name: "recurring_journal_entries");

            migrationBuilder.DropColumn(
                name: "IsYearEndClosing",
                table: "journal_entries");
        }
    }
}
