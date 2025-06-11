using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Migrations
{
    /// <inheritdoc />
    public partial class TestScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoClose",
                table: "Tests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoPublish",
                table: "Tests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsScheduled",
                table: "Tests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndDate",
                table: "Tests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartDate",
                table: "Tests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoClose",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "AutoPublish",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "IsScheduled",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "ScheduledEndDate",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "ScheduledStartDate",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tests");
        }
    }
}
