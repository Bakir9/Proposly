using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandTaskDescriptionForRichText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTasks",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTasks",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50000)",
                oldMaxLength: 50000,
                oldNullable: true);
        }
    }
}
