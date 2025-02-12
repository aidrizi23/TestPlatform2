using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Data.Migrations
{
    /// <inheritdoc />
    public partial class testbynrofquestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MultipleChoiceQuestionsToShow",
                table: "Tests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuestionsToShow",
                table: "Tests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ShortAnswerQuestionsToShow",
                table: "Tests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrueFalseQuestionsToShow",
                table: "Tests",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultipleChoiceQuestionsToShow",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "QuestionsToShow",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "ShortAnswerQuestionsToShow",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "TrueFalseQuestionsToShow",
                table: "Tests");
        }
    }
}
