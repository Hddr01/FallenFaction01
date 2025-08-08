using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookmarkConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmarks_Titles_TitleId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_BookmarkFolders_UserId",
                table: "BookmarkFolders");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BookmarkFolders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "BookmarkFolders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_TitleId",
                table: "Bookmarks",
                columns: new[] { "UserId", "TitleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkFolders_UserId_Name",
                table: "BookmarkFolders",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_Titles_TitleId",
                table: "Bookmarks",
                column: "TitleId",
                principalTable: "Titles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmarks_Titles_TitleId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_TitleId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_BookmarkFolders_UserId_Name",
                table: "BookmarkFolders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "BookmarkFolders");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "BookmarkFolders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkFolders_UserId",
                table: "BookmarkFolders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_Titles_TitleId",
                table: "Bookmarks",
                column: "TitleId",
                principalTable: "Titles",
                principalColumn: "Id");
        }
    }
}
