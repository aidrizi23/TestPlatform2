using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Data.Migrations
{
    /// <inheritdoc />
    public partial class Attemptemail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentName",
                table: "TestAttempts",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "TestAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "TestAttempts");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "TestAttempts",
                newName: "StudentName");
        }
    }
}
