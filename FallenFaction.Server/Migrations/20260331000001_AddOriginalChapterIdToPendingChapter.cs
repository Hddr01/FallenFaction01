using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddOriginalChapterIdToPendingChapter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalChapterId",
                table: "PendingChapters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingChapters_OriginalChapterId",
                table: "PendingChapters",
                column: "OriginalChapterId");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters",
                column: "OriginalChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters");

            migrationBuilder.DropIndex(
                name: "IX_PendingChapters_OriginalChapterId",
                table: "PendingChapters");

            migrationBuilder.DropColumn(
                name: "OriginalChapterId",
                table: "PendingChapters");
        }
    }
}
