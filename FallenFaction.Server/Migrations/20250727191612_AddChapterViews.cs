using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddChapterViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChapterView_AspNetUsers_UserId",
                table: "ChapterView");

            migrationBuilder.DropForeignKey(
                name: "FK_ChapterView_Chapters_ChapterId",
                table: "ChapterView");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChapterView",
                table: "ChapterView");

            migrationBuilder.RenameTable(
                name: "ChapterView",
                newName: "ChapterViews");

            migrationBuilder.RenameIndex(
                name: "IX_ChapterView_UserId",
                table: "ChapterViews",
                newName: "IX_ChapterViews_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ChapterView_ChapterId",
                table: "ChapterViews",
                newName: "IX_ChapterViews_ChapterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChapterViews",
                table: "ChapterViews",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterViews_AspNetUsers_UserId",
                table: "ChapterViews",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterViews_Chapters_ChapterId",
                table: "ChapterViews",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChapterViews_AspNetUsers_UserId",
                table: "ChapterViews");

            migrationBuilder.DropForeignKey(
                name: "FK_ChapterViews_Chapters_ChapterId",
                table: "ChapterViews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChapterViews",
                table: "ChapterViews");

            migrationBuilder.RenameTable(
                name: "ChapterViews",
                newName: "ChapterView");

            migrationBuilder.RenameIndex(
                name: "IX_ChapterViews_UserId",
                table: "ChapterView",
                newName: "IX_ChapterView_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ChapterViews_ChapterId",
                table: "ChapterView",
                newName: "IX_ChapterView_ChapterId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChapterView",
                table: "ChapterView",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterView_AspNetUsers_UserId",
                table: "ChapterView",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChapterView_Chapters_ChapterId",
                table: "ChapterView",
                column: "ChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");
        }
    }
}
