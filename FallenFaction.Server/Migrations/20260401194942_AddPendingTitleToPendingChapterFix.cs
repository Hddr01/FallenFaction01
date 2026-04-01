using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FallenFaction.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingTitleToPendingChapterFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TitleId",
                table: "PendingChapters",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "PendingTitleId",
                table: "PendingChapters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingChapters_PendingTitleId",
                table: "PendingChapters",
                column: "PendingTitleId");

            migrationBuilder.AddForeignKey(
                name: "FK_PendingChapters_PendingTitles_PendingTitleId",
                table: "PendingChapters",
                column: "PendingTitleId",
                principalTable: "PendingTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PendingChapters_PendingTitles_PendingTitleId",
                table: "PendingChapters");

            migrationBuilder.DropIndex(
                name: "IX_PendingChapters_PendingTitleId",
                table: "PendingChapters");

            migrationBuilder.DropColumn(
                name: "PendingTitleId",
                table: "PendingChapters");

            migrationBuilder.AlterColumn<int>(
                name: "TitleId",
                table: "PendingChapters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
