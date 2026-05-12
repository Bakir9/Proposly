using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "Projects",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Projects");
        }
    }
}
