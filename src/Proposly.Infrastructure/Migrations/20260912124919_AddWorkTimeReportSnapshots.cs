using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTimeReportSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualHoursSnapshot",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosingBalanceHours",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ForfeitedHours",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevised",
                table: "Timesheets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalanceHours",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetHoursSnapshot",
                table: "Timesheets",
                type: "numeric(7,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualHoursSnapshot",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ClosingBalanceHours",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ForfeitedHours",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "IsRevised",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "OpeningBalanceHours",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "TargetHoursSnapshot",
                table: "Timesheets");
        }
    }
}
