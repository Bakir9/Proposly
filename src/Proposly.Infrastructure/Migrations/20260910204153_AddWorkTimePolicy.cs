using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTimePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimesheetBreaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LimitValue = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    AcknowledgedById = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetBreaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetBreaches_Timesheets_TimesheetId",
                        column: x => x.TimesheetId,
                        principalTable: "Timesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTimePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Jurisdiction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    HolidayRegionCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaxHoursPerDay = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AveragingWindowWeeks = table.Column<int>(type: "integer", nullable: false),
                    MaxAverageHoursPerWeek = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinDailyRestHours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinWeeklyRestHours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SurplusCapHours = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    DeficitFloorHours = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTimePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkTimePolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AboveHours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinBreakMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakRules_WorkTimePolicies_WorkTimePolicyId",
                        column: x => x.WorkTimePolicyId,
                        principalTable: "WorkTimePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BreakRules_WorkTimePolicyId_AboveHours",
                table: "BreakRules",
                columns: new[] { "WorkTimePolicyId", "AboveHours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetBreaches_TimesheetId",
                table: "TimesheetBreaches",
                column: "TimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTimePolicies_CompanyId_ValidFrom",
                table: "WorkTimePolicies",
                columns: new[] { "CompanyId", "ValidFrom" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreakRules");

            migrationBuilder.DropTable(
                name: "TimesheetBreaches");

            migrationBuilder.DropTable(
                name: "WorkTimePolicies");
        }
    }
}
