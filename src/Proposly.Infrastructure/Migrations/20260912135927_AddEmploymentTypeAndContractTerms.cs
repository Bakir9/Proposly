using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentTypeAndContractTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AbsorbedByLumpSumHours",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoveredByAllInHours",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "EmploymentTerms",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "FullTime");

            migrationBuilder.AddColumn<bool>(
                name: "IsAllIn",
                table: "EmploymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeLumpSumHours",
                table: "EmploymentTerms",
                type: "numeric(6,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsorbedByLumpSumHours",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "CoveredByAllInHours",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "IsAllIn",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "OvertimeLumpSumHours",
                table: "EmploymentTerms");
        }
    }
}
