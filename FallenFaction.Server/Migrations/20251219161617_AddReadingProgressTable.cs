using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddReadingProgressTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReadingProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TitleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastReadChapter = table.Column<int>(type: "int", nullable: false),
                    LastReadDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReadingProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReadingProgress_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgress_TitleId",
                table: "ReadingProgress",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingProgress_UserId_TitleId",
                table: "ReadingProgress",
                columns: new[] { "UserId", "TitleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReadingProgress");
        }
    }
}
