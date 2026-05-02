using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActualHoursToTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualHours",
                table: "ProjectTasks",
                type: "numeric(8,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualHours",
                table: "ProjectTasks");
        }
    }
}
