using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVatSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVatExempt",
                table: "Offers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "VatLabel",
                table: "Offers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "No VAT");

            migrationBuilder.AddColumn<string>(
                name: "VatNote",
                table: "Offers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "Offers",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VatType",
                table: "Offers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Exempt");

            migrationBuilder.AddColumn<string>(
                name: "CompanyCountry",
                table: "Companies",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyVatNumber",
                table: "Companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultVatRate",
                table: "Companies",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatExempt",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVatRegistered",
                table: "Companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VatExemptReason",
                table: "Companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVatExempt",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "VatLabel",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "VatNote",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "VatType",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "CompanyCountry",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CompanyVatNumber",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DefaultVatRate",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsVatExempt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsVatRegistered",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VatExemptReason",
                table: "Companies");
        }
    }
}
