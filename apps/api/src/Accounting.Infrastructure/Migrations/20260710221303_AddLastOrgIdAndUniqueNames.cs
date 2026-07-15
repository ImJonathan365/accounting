using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Accounting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastOrgIdAndUniqueNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tax_rates_OrganizationId",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_products_OrganizationId",
                table: "products");

            migrationBuilder.AddColumn<Guid>(
                name: "LastOrgId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_OrganizationId_Name",
                table: "tax_rates",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_OrganizationId_Name",
                table: "products",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tax_rates_OrganizationId_Name",
                table: "tax_rates");

            migrationBuilder.DropIndex(
                name: "IX_products_OrganizationId_Name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "LastOrgId",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rates_OrganizationId",
                table: "tax_rates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_products_OrganizationId",
                table: "products",
                column: "OrganizationId");
        }
    }
}
