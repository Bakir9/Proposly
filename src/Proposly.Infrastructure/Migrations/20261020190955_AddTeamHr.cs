using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamHr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsProfileOnly",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoDeductBreak",
                table: "EmploymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CoreFrom",
                table: "EmploymentTerms",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CoreTo",
                table: "EmploymentTerms",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedWeeklyHours",
                table: "EmploymentTerms",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Flexitime",
                table: "EmploymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "FridayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HolidaySurchargePct",
                table: "EmploymentTerms",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HomeOfficeDaysPerWeek",
                table: "EmploymentTerms",
                type: "numeric(3,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateAmount",
                table: "EmploymentTerms",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HourlyRateCurrency",
                table: "EmploymentTerms",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxHoursPerDay",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxHoursPerMonth",
                table: "EmploymentTerms",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MondayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NightSurchargePct",
                table: "EmploymentTerms",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnCallDuty",
                table: "EmploymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OvertimeMode",
                table: "EmploymentTerms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "TimeOff");

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeSurchargePct",
                table: "EmploymentTerms",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaturdayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SundayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SundaySurchargePct",
                table: "EmploymentTerms",
                type: "numeric(5,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThursdayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackingRequired",
                table: "EmploymentTerms",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TuesdayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WednesdayHours",
                table: "EmploymentTerms",
                type: "numeric(4,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkModel",
                table: "EmploymentTerms",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "FullTime");

            migrationBuilder.CreateTable(
                name: "EmployeeProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobTitle = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    Svnr = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Location = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EmergencyName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EmergencyRelation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmergencyPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmploymentStart = table.Column<DateOnly>(type: "date", nullable: true),
                    EmploymentEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    PhotoStorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CvJson = table.Column<string>(type: "text", nullable: true),
                    CvFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CvFileSize = table.Column<long>(type: "bigint", nullable: true),
                    CvFileUploadedAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CvStorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAllowances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAllowances_EmployeeProfiles_EmployeeProfileId",
                        column: x => x.EmployeeProfileId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_EmployeeProfiles_EmployeeProfileId",
                        column: x => x.EmployeeProfileId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Pinned = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeNotes_EmployeeProfiles_EmployeeProfileId",
                        column: x => x.EmployeeProfileId,
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAllowances_EmployeeProfileId",
                table: "EmployeeAllowances",
                column: "EmployeeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeProfileId",
                table: "EmployeeDocuments",
                column: "EmployeeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNotes_EmployeeProfileId",
                table: "EmployeeNotes",
                column: "EmployeeProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_CompanyId_UserId",
                table: "EmployeeProfiles",
                columns: new[] { "CompanyId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeAllowances");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "EmployeeNotes");

            migrationBuilder.DropTable(
                name: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "IsProfileOnly",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AutoDeductBreak",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "CoreFrom",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "CoreTo",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "ExpectedWeeklyHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "Flexitime",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "FridayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "HolidaySurchargePct",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "HomeOfficeDaysPerWeek",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "HourlyRateAmount",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "HourlyRateCurrency",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "MaxHoursPerDay",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "MaxHoursPerMonth",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "MondayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "NightSurchargePct",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "OnCallDuty",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "OvertimeMode",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "OvertimeSurchargePct",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "SaturdayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "SundayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "SundaySurchargePct",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "ThursdayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "TrackingRequired",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "TuesdayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "WednesdayHours",
                table: "EmploymentTerms");

            migrationBuilder.DropColumn(
                name: "WorkModel",
                table: "EmploymentTerms");
        }
    }
}
