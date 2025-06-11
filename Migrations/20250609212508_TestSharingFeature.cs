using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestPlatform2.Migrations
{
    /// <inheritdoc />
    public partial class TestSharingFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestShares",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TestId = table.Column<string>(type: "text", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "text", nullable: false),
                    Permission = table.Column<int>(type: "integer", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestShares_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestShares_AspNetUsers_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestShares_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_OwnerUserId",
                table: "TestShares",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_SharedWithUserId",
                table: "TestShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestShares_TestId_SharedWithUserId",
                table: "TestShares",
                columns: new[] { "TestId", "SharedWithUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestShares");
        }
    }
}
