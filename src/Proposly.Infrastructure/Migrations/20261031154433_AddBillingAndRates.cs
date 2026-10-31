using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingAndRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BillRateAmount",
                table: "TimeEntries",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BillRateCurrency",
                table: "TimeEntries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<decimal>(
                name: "CostRateAmount",
                table: "TimeEntries",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CostRateCurrency",
                table: "TimeEntries",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "BillingType",
                table: "Projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FixedPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "ExpenseBudgetAmount",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseBudgetCurrency",
                table: "Projects",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedHours",
                table: "ProjectMembers",
                type: "numeric(9,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BillRateAmount",
                table: "ProjectMembers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BillRateCurrency",
                table: "ProjectMembers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<decimal>(
                name: "CostRateAmount",
                table: "ProjectMembers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CostRateCurrency",
                table: "ProjectMembers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            // Backfill: the single legacy hourly rate becomes both the cost and the bill rate,
            // so every existing report keeps its numbers until real rates are entered.
            migrationBuilder.Sql("""
                UPDATE "ProjectMembers" SET
                    "CostRateAmount" = "HourlyRateAmount",
                    "CostRateCurrency" = "HourlyRateCurrency",
                    "BillRateAmount" = "HourlyRateAmount",
                    "BillRateCurrency" = "HourlyRateCurrency";

                UPDATE "TimeEntries" SET
                    "CostRateAmount" = "HourlyRateAmount",
                    "CostRateCurrency" = "HourlyRateCurrency",
                    "BillRateAmount" = "HourlyRateAmount",
                    "BillRateCurrency" = "HourlyRateCurrency";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillRateAmount",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "BillRateCurrency",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CostRateAmount",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "CostRateCurrency",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "BillingType",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ExpenseBudgetAmount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ExpenseBudgetCurrency",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "AllocatedHours",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "BillRateAmount",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "BillRateCurrency",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "CostRateAmount",
                table: "ProjectMembers");

            migrationBuilder.DropColumn(
                name: "CostRateCurrency",
                table: "ProjectMembers");
        }
    }
}
