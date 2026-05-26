using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanToCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxProjects",
                table: "Companies",
                type: "integer",
                nullable: true,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsers",
                table: "Companies",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanExpiresAt",
                table: "Companies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "Companies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Free");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxProjects",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MaxUsers",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlanExpiresAt",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "Companies");
        }
    }
}
