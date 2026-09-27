using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTimeAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbsenceEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    EntitledDays = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    CarriedOverDays = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbsenceEntitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FirstDayIsHalf = table.Column<bool>(type: "boolean", nullable: false),
                    LastDayIsHalf = table.Column<bool>(type: "boolean", nullable: false),
                    ConsumedDays = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbsenceRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceEntitlements_CompanyId_UserId_Year",
                table: "AbsenceEntitlements",
                columns: new[] { "CompanyId", "UserId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_CompanyId_Status",
                table: "AbsenceRequests",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequests_CompanyId_UserId_StartDate",
                table: "AbsenceRequests",
                columns: new[] { "CompanyId", "UserId", "StartDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceEntitlements");

            migrationBuilder.DropTable(
                name: "AbsenceRequests");
        }
    }
}
