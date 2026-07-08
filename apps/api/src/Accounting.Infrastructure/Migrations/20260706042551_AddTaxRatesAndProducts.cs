using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxRatesAndProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TaxRateId",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tax_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    TaxAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_rates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_rates_accounts_TaxAccountId",
                        column: x => x.TaxAccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tax_rates_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_products_tax_rates_TaxRateId",
                        column: x => x.TaxRateId,
                        principalTable: "tax_rates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_TaxRateId",
                table: "invoice_lines",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_products_AccountId",
                table: "products",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_products_OrganizationId",
                table: "products",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_products_TaxRateId",
                table: "products",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_OrganizationId",
                table: "tax_rates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_TaxAccountId",
                table: "tax_rates",
                column: "TaxAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_lines_tax_rates_TaxRateId",
                table: "invoice_lines",
                column: "TaxRateId",
                principalTable: "tax_rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoice_lines_tax_rates_TaxRateId",
                table: "invoice_lines");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_TaxRateId",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                table: "invoice_lines");
        }
    }
}
