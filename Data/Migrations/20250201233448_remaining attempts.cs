using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Data.Migrations
{
    /// <inheritdoc />
    public partial class remainingattempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RemainingAttempts",
                table: "TestAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingAttempts",
                table: "TestAttempts");
        }
    }
}
