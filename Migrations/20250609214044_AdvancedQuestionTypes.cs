using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Migrations
{
    /// <inheritdoc />
    public partial class AdvancedQuestionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowMultiplePerZone",
                table: "Questions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraggableItemsJson",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropZonesJson",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HotspotsJson",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageHeight",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageWidth",
                table: "Questions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelsJson",
                table: "Questions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrderMatters",
                table: "Questions",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMultiplePerZone",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DraggableItemsJson",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "DropZonesJson",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "HotspotsJson",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageHeight",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ImageWidth",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LabelsJson",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "OrderMatters",
                table: "Questions");
        }
    }
}
