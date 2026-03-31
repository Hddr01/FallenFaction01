using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class nemmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters",
                column: "OriginalChapterId",
                principalTable: "Chapters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingChapters_Chapters_OriginalChapterId",
                table: "PendingChapters",
                column: "OriginalChapterId",
                principalTable: "Chapters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
