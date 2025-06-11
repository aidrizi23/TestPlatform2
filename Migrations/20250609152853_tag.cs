using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Migrations
{
    /// <inheritdoc />
    public partial class tag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Tests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCategories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestTags_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestTagRelations",
                columns: table => new
                {
                    TestId = table.Column<string>(type: "text", nullable: false),
                    TagId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTagRelations", x => new { x.TestId, x.TagId });
                    table.ForeignKey(
                        name: "FK_TestTagRelations_TestTags_TagId",
                        column: x => x.TagId,
                        principalTable: "TestTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestTagRelations_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tests_CategoryId",
                table: "Tests",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCategories_UserId",
                table: "TestCategories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTagRelations_TagId",
                table: "TestTagRelations",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TestTags_UserId",
                table: "TestTags",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tests_TestCategories_CategoryId",
                table: "Tests",
                column: "CategoryId",
                principalTable: "TestCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tests_TestCategories_CategoryId",
                table: "Tests");

            migrationBuilder.DropTable(
                name: "TestCategories");

            migrationBuilder.DropTable(
                name: "TestTagRelations");

            migrationBuilder.DropTable(
                name: "TestTags");

            migrationBuilder.DropIndex(
                name: "IX_Tests_CategoryId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tests");
        }
    }
}
