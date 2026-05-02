using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskDependencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTaskDependencies",
                columns: table => new
                {
                    BlockedTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockingTaskId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskDependencies", x => new { x.BlockedTaskId, x.BlockingTaskId });
                    table.ForeignKey(
                        name: "FK_ProjectTaskDependencies_ProjectTasks_BlockedTaskId",
                        column: x => x.BlockedTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTaskDependencies_ProjectTasks_BlockingTaskId",
                        column: x => x.BlockingTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskDependencies_BlockingTaskId",
                table: "ProjectTaskDependencies",
                column: "BlockingTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTaskDependencies");
        }
    }
}
