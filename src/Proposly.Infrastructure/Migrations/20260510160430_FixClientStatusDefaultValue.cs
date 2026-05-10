using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proposly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixClientStatusDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clients created before AddClientStatus migration ran got Status = '' (the EF default).
            // Correct them to 'Active' so the enum conversion doesn't blow up.
            migrationBuilder.Sql("""
                UPDATE "Clients" SET "Status" = 'Active' WHERE "Status" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
