using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalVoid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "journal_entries",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAtUtc",
                table: "journal_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidedByEntryId",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VoidsEntryId",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_VoidedByEntryId",
                table: "journal_entries",
                column: "VoidedByEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_VoidsEntryId",
                table: "journal_entries",
                column: "VoidsEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_journal_entries_journal_entries_VoidedByEntryId",
                table: "journal_entries",
                column: "VoidedByEntryId",
                principalTable: "journal_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_journal_entries_journal_entries_VoidsEntryId",
                table: "journal_entries",
                column: "VoidsEntryId",
                principalTable: "journal_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_journal_entries_journal_entries_VoidedByEntryId",
                table: "journal_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_journal_entries_journal_entries_VoidsEntryId",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_VoidedByEntryId",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_VoidsEntryId",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "VoidedAtUtc",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "VoidedByEntryId",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "VoidsEntryId",
                table: "journal_entries");
        }
    }
}
